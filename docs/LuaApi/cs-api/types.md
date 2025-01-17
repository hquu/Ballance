# 内置类型

虽然LUA只支持布尔、数字、字符串和表类型，但是C#端的类型更确定，例如 number 在 C# 可能是整型，也可能是浮点float，因此当您使用 Lua 向 C# 传值时，需要注意数字的取值范围，一般情况下无需关心，只是在特殊情况下（比如数字非常大）可能会出现错误的结果。

本表列出了一些Lua类型转换到C#端类型的说明，每个API的文档都会说明它的参数是属于什么类型。

|C#类型|Lua类型|说明|
|---|---|---|
|[ulong](https://docs.microsoft.com/zh-cn/dotnet/api/system.uint64)|number|64位无符号整数|
|[long](https://docs.microsoft.com/zh-cn/dotnet/api/system.int64)|number|64位整数|
|[int](https://docs.microsoft.com/zh-cn/dotnet/api/system.int32)|number|32位整数|
|[uint](https://docs.microsoft.com/zh-cn/dotnet/api/system.uint32)|number|32位无符号整数|
|[float](https://docs.microsoft.com/zh-cn/dotnet/api/system.single)|number|单精度浮点数|
|[double](https://docs.microsoft.com/zh-cn/dotnet/api/system.double)|number|双精度浮点数|
|[byte](https://docs.microsoft.com/zh-cn/dotnet/api/system.byte)|number|表示一个 8 位无符号整数|
|[ushort](https://docs.microsoft.com/zh-cn/dotnet/api/system.uint16)|number|16位无符号整数|
|[short](https://docs.microsoft.com/zh-cn/dotnet/api/system.int16)|number|16位整数|

## Enum 枚举

在C#端的枚举在 Lua 会被转为 `number` 类型。
