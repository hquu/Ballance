# Ballance Rebuild 调试功能

游戏提供了一些调试方法，可以让你制作地图或者关卡时可以方便的调试，以方便您后续开发自定义关卡或者模组。

## 调试指令

在[开启调试模式](./project-help.md#如何开启发行版的调试模式)后，你可以通过按 <kbd>F12</kbd> 呼出调试控制台，在调试控制台中可以输入调试命令执行。

这里列出了一些游戏中常用的调试命令：

* `pass` 直接过关.
* `fall` 触发球掉落死亡。
* `restart` 重新开始当前关卡。
* `pause` 暂停。
* `resume` 恢复。
* `rebirth` 重新出生球（不消耗生命球）。
* `addlife` 添加一个生命球。
* `addtime <number>` 添加时间点。
* `setlife <number>` 设置当前生命球数量。设置 -1 的话就是无限生命。
* `settime <number>` 设置当前时间点数量。
* `gos <number>` 跳转到指定的小节。测试自制地图时很有用。
* `sector next` 进入下一小节。
* `nextlev` 进入下一关。
* `unload` 卸载当前关卡并返回主菜单。
* `sector set <number>` 设置当前激活的小节。测试自制地图时很有用。
* `sector reset <number>` 重置指定的小节机关。
* `sector reset-all <number>` 设置当前激活的小节。
* `clear` 清除控制台输出。

完整的调试命令帮助可以输入 help 查看。

## 调试设置

以下是一些内部设置开关，可以设置，以控制一些功能。

你可以通过指令来设置开关，例如下面的指令设置 `debugDisableLoadUI` 属性为开/关状态：

```
s set core debugDisableLoadUI bool true
s set core debugDisableIntro bool true
s set core debugDisableLoadUI bool false
```

* debugDisableLoadUI [bool] 控制关卡加载器是否禁止显示遮罩UI
* debugDisablePhysicsInfo [bool] 禁止显示物理输出信息UI
* debugDisableBallInfo [bool] 禁止显示球输出信息UI
* debugDisableIntro [bool] 是否关闭Intro动画

## 调试快捷键

在开启调试模式后，你可以通过按下面的快捷键在游戏中调试关卡：

* 按 <kbd>Q</kbd> 可以升高球向上飞
* 按 <kbd>E</kbd> 可以下降球向下飞
* 按 <kbd>1</kbd> 可以设置球为木球
* 按 <kbd>2</kbd> 可以设置球为石球
* 按 <kbd>3</kbd> 可以设置球为纸球
* 按 <kbd>4</kbd> 快速重置到当前小节出生点，不会消耗生命球。
* 按 <kbd>5</kbd> 加一个生命球。
* 按 <kbd>6</kbd> 触发死亡。与fall命令相同。

## 调试 Lua 代码

请参考[模组制作文档](../SystemModding/readme.md)的调试 Lua 代码章节。
