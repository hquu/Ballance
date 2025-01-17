﻿using Ballance2.Utils;

/*
 * Copyright (c) 2020  mengyu
 * 
 * 模块名：     
 * GameCSharpHandler.cs
 * 用途：
 * C#事件或是回调接收器。
 * 
 * 作者：
 * mengyu
 */

namespace Ballance2.Base.Handler
{
  class GameCSharpHandler : GameHandler
  {
    public override bool CallEventHandler(string evtName, params object[] pararms)
    {
      if (Destroyed)
        return false;
      if (eventHandlerDelegate != null)
      {
        if (LuaUtils.CheckParamIsLuaTable(pararms))
          return eventHandlerDelegate.Invoke(evtName, LuaUtils.LuaTableArrayToObjectArray(pararms));
        else
          return eventHandlerDelegate.Invoke(evtName, pararms);
      }
      return base.CallEventHandler(evtName, pararms);
    }
    public override object CallCustomHandler(params object[] pararms)
    {
      if (Destroyed)
        return false;
      if (customHandlerDelegate != null)
      {
        if (LuaUtils.CheckParamIsLuaTable(pararms))
          return customHandlerDelegate.Invoke(LuaUtils.LuaTableArrayToObjectArray(pararms));
        else
          return customHandlerDelegate.Invoke(pararms);
      }
      return base.CallCustomHandler(pararms);
    }
    public GameCSharpHandler(GameEventHandlerDelegate eventHandlerDelegate)
    {
      this.eventHandlerDelegate = eventHandlerDelegate;
    }
    public GameCSharpHandler(GameCustomHandlerDelegate customHandlerDelegate)
    {
      this.customHandlerDelegate = customHandlerDelegate;
    }

    private GameCustomHandlerDelegate customHandlerDelegate = null;
    private GameEventHandlerDelegate eventHandlerDelegate = null;

    public override void Dispose()
    {
      customHandlerDelegate = null;
      eventHandlerDelegate = null;
      base.Dispose();
    }
  }
}
