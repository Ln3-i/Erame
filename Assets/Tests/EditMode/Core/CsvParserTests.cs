using Game.Runtime;
using Game.Runtime.Services;
using System.Collections.Generic;
using NUnit.Framework;
using Erame.Core;

namespace Tests{
    public class CsvParserTests{
        private sealed class ItemRow : IData{
            public int Id { get; set; }
            public string Name = "";
            public string Note = "";
            public int Price;
            public bool Equipped;
        }

        private static void Apply(ItemRow row, string column, string raw){
            switch (column){
                case "Id": row.Id = (int)CsvParser.ConvertValue(raw, typeof(int)); break;
                case "Name": row.Name = (string)CsvParser.ConvertValue(raw, typeof(string)); break;
                case "Note": row.Note = (string)CsvParser.ConvertValue(raw, typeof(string)); break;
                case "Price": row.Price = (int)CsvParser.ConvertValue(raw, typeof(int)); break;
                case "Equipped": row.Equipped = (bool)CsvParser.ConvertValue(raw, typeof(bool)); break;
            }
        }

        private static DataTable<ItemRow> Parse(string csv, FakeLogService log = null){
            return CsvParser.Parse(csv, log ?? new FakeLogService(), () => new ItemRow(), Apply);
        }

        [Test]
        public void Parse_HeaderAndRows_FillsColumns(){
            var table = Parse("Id,Name,Price,Equipped\n1,Sword,100,1\n2,Shield,50,0\n");

            Assert.AreEqual(2, table.Count);
            Assert.AreEqual("Sword", table.Get(1).Name);
            Assert.AreEqual(100, table.Get(1).Price);
            Assert.IsTrue(table.Get(1).Equipped);
            Assert.AreEqual("Shield", table.Get(2).Name);
            Assert.IsFalse(table.Get(2).Equipped);
        }

        [Test]
        public void Parse_QuotedFields_KeepsSeparatorNewlineAndEscapedQuote(){
            var table = Parse(
                "Id,Name,Note\n" +
                "1,\"hello, world\",\"line1\nline2\"\n" +
                "2,\"say \"\"hi\"\"\",x\n");

            Assert.AreEqual(2, table.Count);
            Assert.AreEqual("hello, world", table.Get(1).Name);
            Assert.AreEqual("line1\nline2", table.Get(1).Note);
            Assert.AreEqual("say \"hi\"", table.Get(2).Name);
        }

        [Test]
        public void Parse_SkipsEmptyLines(){
            var table = Parse("Id,Name\n1,A\n\n   \n2,B\n");
            Assert.AreEqual(2, table.Count);
        }

        [Test]
        public void Parse_EmptyOrNullInput_ReturnsEmptyTable(){
            Assert.AreEqual(0, Parse("").Count);
            Assert.AreEqual(0, Parse("   \n  ").Count);
            Assert.AreEqual(0, Parse("Id,Name\n").Count); // 只有表头
        }

        [Test]
        public void Parse_UnclosedQuote_ThrowsInvalidData(){
            Assert.Throws<System.IO.InvalidDataException>(() => Parse("Id,Name\n1,\"oops"));
        }

        [Test]
        public void Parse_DuplicateId_KeepsFirstRowAndWarns(){
            var log = new FakeLogService();
            var table = Parse("Id,Name\n1,first\n1,second\n", log);

            Assert.AreEqual(1, table.Count);
            Assert.AreEqual("first", table.Get(1).Name);
            Assert.AreEqual(1, log.Warnings.Count);
            StringAssert.Contains("重复", log.Warnings[0]);
        }

        [Test]
        public void Parse_CellConvertFailure_WarnsButKeepsOtherColumns(){
            var log = new FakeLogService();
            var table = Parse("Id,Name\n1,ok\nnotanumber,bad\n", log);

            Assert.AreEqual(2, table.Count); // Id 解析失败的行仍被添加（Id 为默认值）
            Assert.AreEqual(0, table.Get(0).Id);
            Assert.AreEqual("bad", table.Get(0).Name);
            Assert.AreEqual(1, log.Warnings.Count);
        }

        [Test]
        public void ConvertValue_CommonTypes(){
            Assert.AreEqual(42, CsvParser.ConvertValue("42", typeof(int)));
            Assert.AreEqual(1.5f, CsvParser.ConvertValue("1.5", typeof(float)));
            Assert.AreEqual("abc", CsvParser.ConvertValue("abc", typeof(string)));
            Assert.IsTrue((bool)CsvParser.ConvertValue("1", typeof(bool)));
            Assert.IsTrue((bool)CsvParser.ConvertValue("true", typeof(bool)));
            Assert.IsFalse((bool)CsvParser.ConvertValue("0", typeof(bool)));
            Assert.IsFalse((bool)CsvParser.ConvertValue("false", typeof(bool)));
        }

        [Test]
        public void ConvertValue_EmptyCell_ReturnsDefaults(){
            Assert.AreEqual(0, CsvParser.ConvertValue("", typeof(int)));
            Assert.AreEqual(0f, CsvParser.ConvertValue("", typeof(float)));
            Assert.AreEqual(false, CsvParser.ConvertValue("", typeof(bool)));
            Assert.IsNull(CsvParser.ConvertValue("", typeof(string)));
        }
    }
}
