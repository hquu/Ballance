﻿# Ballance2.Services.I18N.I18N 
国际化字符串提供类，可快速获取当前游戏语言的对应本地化字符串。

## 注解


要添加你的国际化字符串，有两种方式：
* 在你的模组包目录下添加 `PackageLanguageRes.xml` 文件，模块包在加载时会自动加载字符串文件数据进入系统。
* 手动调用 `I18NProvider.LoadLanguageResources` 加载国际化字符串文件。

国际化字符串文件 xml 的格式是：
```xml
<I18n>
  <Language name="ChineseSimplified">
    <Text name="core.ui.RestartLevel">重新开始关卡</Text>
  </Language>
  <Language name="English">
    <Text name="core.ui.RestartLevel">Restart Level</Text>
  </Language>
  <Language name="ChineseTraditional">
    <Text name="core.ui.RestartLevel">重新開始關卡</Text>  </Language>
</I18n>
```

Language 的 name 是定义在 `UnityEngine.SystemLanguage` 中，你可以设置多个语言。

?> **提示：** Text 的 name 是整个游戏唯一的，所以建议每个模组使用自己独特的前缀，防止与他人冲突。




## 方法



### `静态` Tr(key)

获取国际化字符串


#### 参数


`key` string <br/>字符串键



#### 返回值

string <br/>返回找到的字符串，如果未找到，则返回 [Key xxx not found!]


### `静态` Tr(key, defaultString)

获取国际化字符串


#### 参数


`key` string <br/>字符串键

`defaultString` string <br/>如果没有找到指定的字符串国际化信息，则返回此默认字符串



#### 返回值

string <br/>


### `静态` TrF(key, formatParams)

获取国际化字符串并自定义格式化参数


#### 参数


`key` string <br/>字符串键

`formatParams` [Object[]](https://docs.microsoft.com/zh-cn/dotnet/api/System.Object[]) <br/>要自定义格式化的参数



#### 返回值

string <br/>


### `静态` TrF(key, defaultString, formatParams)

获取国际化字符串并自定义格式化参数


#### 参数


`key` string <br/>字符串键

`defaultString` string <br/>如果没有找到指定的字符串国际化信息，则使用此此默认字符串代替

`formatParams` [Object[]](https://docs.microsoft.com/zh-cn/dotnet/api/System.Object[]) <br/>要自定义格式化的参数



#### 返回值

string <br/>