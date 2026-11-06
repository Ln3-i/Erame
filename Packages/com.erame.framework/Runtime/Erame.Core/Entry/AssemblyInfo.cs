using System.Runtime.CompilerServices;

// 让 Erame.Runtime 在不扩大公共 API 的前提下注入入口的容器（Container 的 internal set）。
[assembly: InternalsVisibleTo("Erame.Runtime")]
