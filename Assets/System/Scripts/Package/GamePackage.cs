﻿using Ballance2.Base.Handler;
using Ballance2.Config;
using Ballance2.Services;
using Ballance2.Services.Debug;
using Ballance2.Services.I18N;
using Ballance2.Services.LuaService.Lua;
using Ballance2.Services.LuaService.LuaWapper;
using Ballance2.Utils;
using SLua;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using UnityEngine;
using UnityEngine.Profiling;

/*
* Copyright(c) 2021  mengyu
*
* 模块名：     
* GamePackage.cs
* 
* 用途：
* 游戏模块的声明以及模块功能提供。
* 负责：模块运行环境初始化卸载、资源读取相关。
*
* 作者：
* mengyu
*/

namespace Ballance2.Package
{
  /// <summary>
  /// 模块包实例
  /// </summary>
  [SLua.CustomLuaClass]
  [LuaApiDescription("模块包实例")]
  [LuaApiNotes(@"这是游戏 Lua 模组的主要承载类，主要负责：模块运行环境初始化卸载、资源读取相关。")]
  public class GamePackage
  {
    /// <summary>
    /// 标签
    /// </summary>
    [LuaApiDescription("标签")]
    public string TAG
    {
      get { return "GamePackage:" + PackageName; }
    }

    internal int DependencyRefCount = 0;
    internal bool UnLoadWhenDependencyRefNone = false;

    [DoNotToLua]
    public virtual Task<bool> LoadInfo(string filePath)
    {
      PackageFilePath = filePath;
      var t = new Task<bool>(() => { return true; });
      t.Start();
      return t;
    }
    [DoNotToLua]
    public virtual async Task<bool> LoadPackage()
    {
      //FixBundleShader();
      LoadI18NResource();

      //模块代码环境初始化
      if (Type == GamePackageType.Module)
        return await LoadPackageCodeBase();
      return true;
    }
    [DoNotToLua]
    public virtual void Destroy()
    {
      Log.D(TAG, "Destroy package {0}", PackageName);

      if(!IsUnloadCodeExecuted())
        RunPackageBeforeUnLoadCode();

      //GameManager.DestroyManagersInMod(PackageName);
      HandlerClear();

      if (requiredLuaClasses != null)
      {
        requiredLuaClasses.Clear();
        requiredLuaClasses = null;
      }
      if (requiredLuaFiles != null)
      {
        requiredLuaFiles.Clear();
        requiredLuaFiles = null;
      }
      //TODO: CLEAR
      luaObjects.Clear();

      //释放AssetBundle
      if (AssetBundle != null)
      {
        AssetBundle.Unload(true);
        AssetBundle = null;
      }

      if (CSharpAssembly != null)
        CSharpAssembly = null;
      if (PackageEntry != null)
        PackageEntry = null;
    }

    #region 系统包

    private static GamePackage _CorePackage = null;
    private static GamePackage _SystemPackage = new GameSystemPackage();

    /// <summary>
    /// 设置核心的模块包
    /// </summary>
    /// <returns></returns>
    internal static void SetCorePackage(GamePackage pack) { 
      _CorePackage = pack; 
    }
    /// <summary>
    /// 获取 Ballance 核心的模块包。核心模块包包名是 core。
    /// </summary>
    /// <returns></returns>
    [LuaApiDescription("获取 Ballance 核心的模块包。核心模块包包名是 core。")]
    [LuaApiNotes("Ballance 核心模块包是 Ballance 游戏的主要模块，所有游戏代码与资源均在这个包中，是所有模组的依赖。")]
    public static GamePackage GetCorePackage() { 
      return _CorePackage; 
    }
    /// <summary>
    /// 获取系统核心的模块包，包名是 system 。
    /// </summary>
    /// <returns></returns>
    [LuaApiDescription("获取系统核心的模块包，包名是 system 。")]
    [LuaApiNotes("系统核心模块包存放了一些系统初始化脚本、工具脚本等等，是 Ballance 核心模块包的依赖。")]
    public static GamePackage GetSystemPackage() { 
      return _SystemPackage; 
    }

    #endregion

    #region 常量定义

    public const int FLAG_CODE_BASE_LOADED = 0x000000001;
    public const int FLAG_CODE_LUA_PACK = 0x000000002;
    public const int FLAG_CODE_CS_PACK = 0x000000004;
    public const int FLAG_CODE_ENTRY_CODE_RUN = 0x000000008;
    public const int FLAG_CODE_UNLOD_CODE_RUN = 0x000000010;
    public const int FLAG_PACK_NOT_UNLOADABLE = 0x000000020;
    public const int FLAG_PACK_SYSTEM_PACKAGE = 0x000000040;

    #endregion

    #region 模块运行环境

    /// <summary>
    /// C# 程序集, 如果当前模组设置加载了 C# 模块，则可以在这里访问程序集。
    /// </summary>
    [LuaApiDescription("C# 程序集, 如果当前模组设置加载了 C# 模块，则可以在这里访问程序集。")]
    public Assembly CSharpAssembly { get; protected set; }
    /// <summary>
    /// 程序入口
    /// </summary>
    [LuaApiNoDoc()]
    public GamePackageEntry PackageEntry = new GamePackageEntry();
    
    internal int flag = 0;

    /// <summary>
    /// 获取是否可以卸载
    /// </summary>
    /// <returns></returns>
    [LuaApiDescription("获取是否可以卸载")]
    public bool IsNotUnLoadable() { return (flag & FLAG_PACK_NOT_UNLOADABLE) == FLAG_PACK_NOT_UNLOADABLE; }
    /// <summary>
    /// 获取是否是系统包
    /// </summary>
    /// <returns></returns>
    [LuaApiDescription("获取是否是系统包")]
    public bool IsSystemPackage() { return (flag & FLAG_PACK_SYSTEM_PACKAGE) == FLAG_PACK_SYSTEM_PACKAGE; }
    /// <summary>
    /// 获取入口代码是否已经运行过
    /// </summary>
    /// <returns></returns>
    [LuaApiDescription("获取入口代码是否已经运行过")]
    public bool IsEntryCodeExecuted() { return (flag & FLAG_CODE_ENTRY_CODE_RUN) == FLAG_CODE_ENTRY_CODE_RUN; }
    /// <summary>
    /// 获取出口代码是否已经运行过
    /// </summary>
    /// <returns></returns>
    [LuaApiDescription("获取出口代码是否已经运行过")]
    public bool IsUnloadCodeExecuted() { return (flag & FLAG_CODE_UNLOD_CODE_RUN) == FLAG_CODE_UNLOD_CODE_RUN; }

    /// <summary>
    /// 设置当前模块的标志位
    /// </summary>
    /// <param name="flag">标志位，（GamePackage.FLAG_*）</param>
    [LuaApiDescription("设置当前模块的标志位")]
    [LuaApiParamDescription("flag", "标志位（GamePackage.FLAG_*）")]
    public void SetFlag(int flag)  {

      if((this.flag & FLAG_PACK_NOT_UNLOADABLE) == FLAG_PACK_NOT_UNLOADABLE && (flag & FLAG_PACK_NOT_UNLOADABLE) != FLAG_PACK_NOT_UNLOADABLE) {
        Log.E(TAG, "Not allow set FLAG_PACK_NOT_UNLOADABLE flag for not unloadable packages.");
        flag |= FLAG_PACK_NOT_UNLOADABLE;
      }
      if((this.flag & FLAG_PACK_SYSTEM_PACKAGE) == FLAG_PACK_SYSTEM_PACKAGE && (flag & FLAG_PACK_NOT_UNLOADABLE) != FLAG_PACK_NOT_UNLOADABLE) {
        Log.E(TAG, "Not allow set FLAG_PACK_NOT_UNLOADABLE flag for not system packages.");
        flag |= FLAG_PACK_NOT_UNLOADABLE;
      }
      this.flag = flag;
    }
    /// <summary>
    /// 获取当前模块的标志位
    /// </summary>
    [LuaApiDescription("获取当前模块的标志位")]
    public int GetFlag() { return flag; }

    /// <summary>
    /// Lua 虚拟机
    /// </summary>
    [DoNotToLua]
    public LuaState PackageLuaState => GameManager.Instance.GameMainLuaState;

    /// <summary>
    /// 加载运行环境代码
    /// </summary>
    /// <returns></returns>
    protected async Task<bool> LoadPackageCodeBase() {
      //判断是否初始化
      if((FLAG_CODE_BASE_LOADED & flag) == FLAG_CODE_BASE_LOADED) {
        Log.E(TAG, "不能重复初始化");
        return false;
      }

      if (ContainCSharp)
      {
        var pm = GameSystem.GetSystemService("GamePackageManager") as GamePackageManager;

        //加载C#程序集是危险的操作，需要询问
        if(!pm.IsTrustPackage(PackageName)) {
          //显示对话框
          pm.ShowTrustPackageDialog(this);
          //等待
          await new WaitUntil(pm.IsTrustPackageDialogFinished);
          //获取结果
          if(!pm.GetTrustPackageDialogResult()) {
            //用户拒绝了加载
            GameErrorChecker.LastError = GameError.AccessDenined;
            Log.E(TAG, "[C#] 用户拒绝了加载模块 " + PackageName);
            return false;
          }
        }

        //加载C#程序集
        CSharpAssembly = LoadCodeCSharp(PackageName + ".dll");
        if (CSharpAssembly == null) {
          Log.E(TAG, "[C#] 无法加载DLL：" + PackageName + ".dll");
          return false;
        }
        
        Type type = CSharpAssembly.GetType("PackageEntry");
        if (type == null)
        {
          Log.W(TAG, "[C#] 未找到 PackageEntry ");
          GameErrorChecker.LastError = GameError.ClassNotFound;
          return false;
        }
        else
        {
          object CSharpPackageEntry = Activator.CreateInstance(type);
          MethodInfo methodInfo = type.GetMethod("Main");  //根据方法名获取MethodInfo对象
          if (type == null)
          {
            Log.W(TAG, "[C#] 未找到 PackageEntry.Main()");
            GameErrorChecker.LastError = GameError.FunctionNotFound;
          } 
          else  
          {
            object b = methodInfo.Invoke(CSharpPackageEntry, new object[] { this });
            if (b is bool) 
            {
              if(!((bool)b))
              {
                Log.W(TAG, "[C#] 模块 PackageEntry.Main 返回了错误");
                GameErrorChecker.LastError = GameError.ExecutionFailed;
                return false;
              }
            }
          }
        }
        
        flag |= FLAG_CODE_CS_PACK;
      }
      
      requiredLuaFiles = new Dictionary<string, object>();
      requiredLuaClasses = new Dictionary<string, LuaFunction>();

      if(PackageName != GamePackageManager.SYSTEM_PACKAGE_NAME) {
        object b = PackageLuaState.doString(@"IntneralLoadLuaPackage('" + PackageName + "','" + EntryCode + "')", "LoadPackageCodeBase(" + TAG + ")");
        if (b is bool && !((bool)b))
        {
          Log.E(TAG, "模块初始化返回了错误");
          GameErrorChecker.LastError = GameError.ExecutionFailed;
          return false;
        }
      }

      flag |= FLAG_CODE_LUA_PACK;
      flag |= FLAG_CODE_BASE_LOADED;
      
      return true;
    }    

    /// <summary>
    /// 运行模块初始化代码，模块的 初始化代码 只能运行一次，不能重复运行。
    /// </summary>
    /// <returns>返回是否成功</returns>
    [LuaApiDescription("运行模块初始化代码，模块的 初始化代码 只能运行一次，不能重复运行。", "返回是否成功")]
    public bool RunPackageExecutionCode()
    {
      if (Type != GamePackageType.Module)
      {
        GameErrorChecker.LastError = GameError.PackageCanNotRun;
        return false;
      }
      if (IsEntryCodeExecuted()) {
        GameErrorChecker.SetLastErrorAndLog(GameError.ExecutionFailed, TAG, "Run ExecutionCode failed, an not run twice");
        return false;
      }

      flag |= FLAG_CODE_ENTRY_CODE_RUN;
      flag ^= FLAG_CODE_UNLOD_CODE_RUN;

      if(PackageEntry.OnLoad != null) {
        
        Profiler.BeginSample(TAG + "PackageEntry.OnLoad");

        bool result = PackageEntry.OnLoad.Invoke(this);

        Profiler.EndSample();

        return result;
      }
      return true;
    }
    /// <summary>
    /// 运行模块卸载回调，模块的 卸载回调 只能运行一次，不能重复运行。
    /// </summary>
    /// <returns>返回是否成功</returns>
    [LuaApiDescription("运行模块卸载回调，模块的 卸载回调 只能运行一次，不能重复运行。", "返回是否成功")]
    public bool RunPackageBeforeUnLoadCode()
    {
      if (Type != GamePackageType.Module)
      {
        GameErrorChecker.LastError = GameError.PackageCanNotRun;
        return false;
      }
      if (IsUnloadCodeExecuted()) {
        GameErrorChecker.SetLastErrorAndLog(GameError.ExecutionFailed, TAG, "Run BeforeUnLoadCode failed, an not run twice");
        return false;
      }

      flag |= FLAG_CODE_UNLOD_CODE_RUN;
      flag ^= FLAG_CODE_ENTRY_CODE_RUN;

      if(PackageEntry.OnBeforeUnLoad != null) {
        
        Profiler.BeginSample(TAG + "PackageEntry.OnBeforeUnLoad");
        
        bool result = PackageEntry.OnBeforeUnLoad.Invoke(this);

        Profiler.EndSample();

        return result;
      }
      return true;
    }

    #region LUA 文件导入

    /// <summary>
    /// 导入 Lua 类到当前模块虚拟机中。
    /// 注意，类函数以 “CreateClass:类名” 开头，
    /// 关于 Lua 类，请参考 docs/SystemModding/lua-class.md 。
    /// </summary>
    /// <param name="className">类名</param>
    /// <returns>类创建函数</returns>
    /// <exception cref="MissingReferenceException">
    /// 如果没有在当前模块包中找到类文件或是类创建函数 CreateClass:* ，则抛出 MissingReferenceException 异常。
    /// </exception>
    /// <exception cref="Exception">
    /// 如果Lua执行失败，则抛出此异常。
    /// </exception>
    [LuaApiDescription("导入 Lua 类到当前模块虚拟机中", "类创建函数")]
    [LuaApiParamDescription("className", "类名")]
    [LuaApiNotes(@"注意，类函数以 `CreateClass:类名` 开头，关于 Lua 类的说明，请参考 [LuaClass](SystemModding/lua-class.md) 。")]
    [LuaApiException("MissingReferenceException", "如果没有在当前模块包中找到类文件或是类创建函数 CreateClass:* ，则抛出 MissingReferenceException 异常。")]
    [LuaApiException("Exception", "如果Lua执行失败，则抛出此异常。")]
    public LuaFunction RequireLuaClass(string className)
    {

        LuaFunction classInit;
        if (requiredLuaClasses.TryGetValue(className, out classInit))
            return classInit;

        var CreateClass = (PackageLuaState["CreateClass"] as LuaTable);
        if(CreateClass == null)
            throw new MissingReferenceException("This shouldn't happen: CreateClass is null! ");

        classInit = CreateClass[className] as LuaFunction;
        if (classInit != null)
        {
            requiredLuaClasses.Add(className, classInit);
            return classInit;
        }

        byte[] lua = TryLoadLuaCodeAsset(className, out var realPath);
        if(lua.Length == 0)
            throw new MissingReferenceException(PackageName + " 无法导入 Lua class \"" + className + "\" : 该文件为空");
        try
        {
          Profiler.BeginSample(TAG + "RequireLuaClass(" + className + ") doBuffer");
          PackageLuaState.doBuffer(lua, realPath/*PackageName + ":" + className*/, out var v);
          Profiler.EndSample();
        }
        catch (Exception e)
        {
            Log.E(TAG, e.ToString());
            GameErrorChecker.LastError = GameError.ExecutionFailed;

            throw new Exception(PackageName + " 无法导入 Lua class \"" + className + "\" : " + e.Message);
        }

        classInit = CreateClass[className] as LuaFunction;
        if (classInit == null)
        {
            throw new MissingReferenceException(PackageName + " 无法导入 Lua class \"" + className + "\" : 未找到初始类函数: CreateClass:" + className);
        }

        requiredLuaClasses.Add(className, classInit);

        return classInit;
    }
    /// <summary>
    /// 导入Lua文件到当前模块虚拟机中
    /// </summary>
    /// <param name="fileName">LUA文件名</param>
    /// <returns>返回执行结果</returns>
    /// <exception cref="MissingReferenceException">
    /// 如果没有在当前模块包中找到Lua文件，则抛出 MissingReferenceException 异常。
    /// </exception>
    /// <exception cref="Exception">
    /// 如果Lua执行失败，则抛出此异常。
    /// </exception>
    [LuaApiDescription("导入Lua文件到当前模块虚拟机中。不重复导入", "返回执行结果")]
    [LuaApiParamDescription("fileName", "LUA文件名")]
    [LuaApiException("MissingReferenceException", "如果没有在当前模块包中找到Lua文件，则抛出 MissingReferenceException 异常。")]
    [LuaApiException("Exception", "如果Lua执行失败，则抛出此异常。")]
    public object RequireLuaFile(string fileName) { return RequireLuaFileInternal(this, fileName, true); }
    /// <summary>
    /// 导入Lua文件到当前模块虚拟机中，允许重复导入执行。
    /// </summary>
    /// <param name="fileName">LUA文件名</param>
    /// <returns>返回执行结果</returns>
    /// <exception cref="MissingReferenceException">
    /// 如果没有在当前模块包中找到Lua文件，则抛出 MissingReferenceException 异常。
    /// </exception>
    /// <exception cref="Exception">
    /// 如果Lua执行失败，则抛出此异常。
    /// </exception>
    [LuaApiDescription("导入Lua文件到当前模块虚拟机中，允许重复导入执行。", "返回执行结果")]
    [LuaApiParamDescription("fileName", "LUA文件名")]
    [LuaApiException("MissingReferenceException", "如果没有在当前模块包中找到Lua文件，则抛出 MissingReferenceException 异常。")]
    [LuaApiException("Exception", "如果Lua执行失败，则抛出此异常。")]
    public object RequireLuaFileNoOnce(string fileName) { return RequireLuaFileInternal(this, fileName, false); }
    /// <summary>
    /// 从其他模块导入Lua文件到当前模块虚拟机中。
    /// </summary>
    /// <param name="otherPack">要导入Lua文件所属模块实例</param>
    /// <param name="fileName">LUA文件名</param>
    /// <returns>返回执行结果</returns>
    /// <exception cref="MissingReferenceException">
    /// 如果没有在当前模块包中找到Lua文件，则抛出 MissingReferenceException 异常。
    /// </exception>
    /// <exception cref="Exception">
    /// 如果Lua执行失败，则抛出此异常。
    /// </exception>
    [LuaApiDescription("从其他模块导入Lua文件到当前模块虚拟机中。", "返回执行结果")]
    [LuaApiParamDescription("otherPack", "要导入Lua文件所属模块实例")]
    [LuaApiParamDescription("fileName", "LUA文件名")]
    [LuaApiException("MissingReferenceException", "如果没有在当前模块包中找到Lua文件，则抛出 MissingReferenceException 异常。")]
    [LuaApiException("Exception", "如果Lua执行失败，则抛出此异常。")]
    public object RequireLuaFile(GamePackage otherPack, string fileName) { return RequireLuaFileInternal(otherPack, fileName, true); }
    /// <summary>
    /// 从其他模块导入Lua文件到当前模块虚拟机中，允许重复导入
    /// </summary>
    /// <param name="otherPack">要导入Lua文件所属模块实例</param>
    /// <param name="fileName">LUA文件名</param>
    /// <returns>返回执行结果</returns>
    /// <exception cref="MissingReferenceException">
    /// 如果没有在指定模块包中找到Lua文件，则抛出 MissingReferenceException 异常。
    /// </exception>
    /// <exception cref="Exception">
    /// 如果Lua执行失败，则抛出此异常。
    /// </exception>
    [LuaApiDescription("从其他模块导入Lua文件到当前模块虚拟机中，允许重复导入", "返回执行结果")]
    [LuaApiParamDescription("fileName", "LUA文件名")]
    [LuaApiParamDescription("otherPack", "要导入Lua文件所属模块实例")]
    [LuaApiException("MissingReferenceException", "如果没有在当前模块包中找到Lua文件，则抛出 MissingReferenceException 异常。")]
    [LuaApiException("Exception", "如果Lua执行失败，则抛出此异常。")]
    public object RequireLuaFileNoOnce(GamePackage otherPack, string fileName) { return RequireLuaFileInternal(otherPack, fileName, false); }
    
    private Dictionary<string, object> requiredLuaFiles = null;
    private Dictionary<string, LuaFunction> requiredLuaClasses = null;

    private byte[] TryLoadLuaCodeAsset(string className, out string realPath) {

        Profiler.BeginSample(TAG + "TryLoadLuaCodeAsset." + className);
        var lua = GetCodeAsset(className);
        if (lua == null) 
            lua = GetCodeAsset(className + ".lua");
        if (lua == null) 
            lua = GetCodeAsset(className + ".luac");
        if (lua == null) 
            lua = GetCodeAsset("Scripts/" + className);
        if (lua == null) 
            lua = GetCodeAsset("Scripts/" + className + ".lua");
        if (lua == null) 
            lua = GetCodeAsset("Scripts/" + className + ".luac");
        if (lua == null)
            throw new MissingReferenceException(PackageName + " 无法导入 " + className + " , 未找到文件");
        realPath = lua.realPath;
        Profiler.EndSample();
        return lua.data;
    }
    private object RequireLuaFileInternal(GamePackage pack, string fileName, bool once)
    {
        object rs = null;
        byte[] lua = pack.TryLoadLuaCodeAsset(fileName, out var realPath);
        if (lua.Length == 0)
            throw new EmptyFileException(PackageName + " 无法导入 Lua \"" + fileName + "\" : 该文件为空");
        try
        {
            //不重复导入
            if(once && requiredLuaFiles.TryGetValue(realPath, out var lastRet)) 
                return lastRet;

            Profiler.BeginSample(TAG + "RequireLuaFileInternal.doBuffer(" + fileName + ")");
            bool rss = PackageLuaState.doBuffer(lua, realPath, out var v);
            Profiler.EndSample();

            if(rss)
                rs = v;
            else
                throw new Exception(PackageName + " 无法导入 Lua \"" + fileName + "\" : 执行失败");

            //添加结果，用于下一次不重复导入
            if(requiredLuaFiles.ContainsKey(realPath))
                requiredLuaFiles[realPath] = rs;
            else
                requiredLuaFiles.Add(realPath, rs);
        }
        catch (Exception e)
        {
          string err = e.ToString();
          Log.E(TAG, err);
          GameErrorChecker.LastError = GameError.ExecutionFailed;

          if(err.Contains("bad header in precompiled chunk")) {
            Log.D(TAG, "Check code bytes\n" + DebugUtils.PrintBytes(lua));
          }

          throw new Exception(PackageName + " 无法导入 Lua \"" + fileName + "\" : " + e.Message);
        }

        return rs;
    }

    #endregion

    #region LUA 函数调用

    /// <summary>
    /// 获取当前 模块主代码 的指定函数
    /// </summary>
    /// <param name="funName">函数名</param>
    /// <returns>返回函数，未找到返回null</returns>
    [LuaApiDescription("获取当前 模块主代码 的指定函数", "返回函数，未找到返回null")]
    [LuaApiParamDescription("funName", "函数名")]
    public LuaFunction GetLuaFun(string funName)
    {
        if (PackageLuaState == null)
        {
            Log.E(TAG, "GetLuaFun Failed because package cannot run");
            GameErrorChecker.LastError = GameError.PackageCanNotRun;
            return null;
        }
        return PackageLuaState.getFunction(funName);
    }
    /// <summary>
    /// 调用模块主代码的lua无参函数
    /// </summary>
    /// <param name="funName">lua函数名称</param>
    [LuaApiDescription("调用模块主代码的lua无参函数")]
    [LuaApiParamDescription("funName", "lua函数名称")]
    public void CallLuaFun(string funName)
    {
        LuaFunction f = GetLuaFun(funName);
        if (f != null) f.call();
        else Log.E(TAG, "CallLuaFun Failed because function {0} not founnd", funName);
    }
    /// <summary>
    /// 尝试调用模块主代码的lua无参函数
    /// </summary>
    /// <param name="funName">lua函数名称</param>
    /// <returns>如果调用成功则返回true，否则返回false</returns>
    [LuaApiDescription("尝试调用模块主代码的lua无参函数", "如果调用成功则返回true，否则返回false")]
    [LuaApiParamDescription("funName", "lua函数名称")]
    public bool TryCallLuaFun(string funName)
    {
        LuaFunction f = GetLuaFun(funName);
        if (f != null) {
            f.call();
            return true;
        }
        return false;
    }
    /// <summary>
    /// 调用模块主代码的lua函数
    /// </summary>
    /// <param name="funName">lua函数名称</param>
    /// <param name="pararms">参数</param>
    [LuaApiDescription("调用模块主代码的lua函数")]
    [LuaApiParamDescription("funName", "lua函数名称")]
    [LuaApiParamDescription("pararms", "参数数组")]
    public void CallLuaFun(string funName, params object[] pararms)
    {
        LuaFunction f = GetLuaFun(funName);
        if (f != null) f.call(pararms);
        else Log.E(TAG, "CallLuaFun Failed because function {0} not founnd", funName);
    }
    /// <summary>
    /// 调用指定的 GameLuaObjectHost 脚本中的lua无参函数
    /// </summary>
    /// <param name="luaObjectName">GameLuaObjectHost 脚本名称</param>
    /// <param name="funName">lua函数名称</param>
    [LuaApiDescription("调用指定的GameLuaObjectHost脚本中的lua无参函数")]
    [LuaApiParamDescription("luaObjectName", "GameLuaObjectHost脚本名称")]
    [LuaApiParamDescription("funName", "lua函数名称")]
    public void CallLuaFun(string luaObjectName, string funName)
    {
        GameLuaObjectHost targetObject = null;
        if (FindLuaObject(luaObjectName, out targetObject))
            targetObject.CallLuaFun(funName);
        else Log.E(TAG, "CallLuaFun Failed because object {0} not founnd", luaObjectName);
    }
    /// <summary>
    /// 调用指定的GameLuaObjectHost脚本中的lua函数
    /// </summary>
    /// <param name="luaObjectName">GameLuaObjectHost脚本名称</param>
    /// <param name="funName">lua函数名称</param>
    /// <param name="pararms">参数</param>
    /// <returns>Lua函数返回的对象，如果调用该函数失败，则返回null</returns>
    [LuaApiDescription("调用指定的GameLuaObjectHost脚本中的lua函数", "Lua函数返回的对象，如果调用该函数失败，则返回null")]
    [LuaApiParamDescription("luaObjectName", "GameLuaObjectHost脚本名称")]
    [LuaApiParamDescription("funName", "lua函数名称")]
    [LuaApiParamDescription("pararms", "参数")]
    public object CallLuaFunWithParam(string luaObjectName, string funName, params object[] pararms)
    {
        GameLuaObjectHost targetObject = null;
        if (FindLuaObject(luaObjectName, out targetObject))
            return targetObject.CallLuaFunWithParam(funName, pararms);
        else 
            Log.E(TAG, "CallLuaFun Failed because object {0} not founnd", luaObjectName);
        return null;
    }

    #endregion

    #region LUA 组件

    //管理当前模块下的所有GameLuaObjectHost脚本，统一管理、释放
    private List<GameLuaObjectHost> luaObjects = new List<GameLuaObjectHost>();

    /// <summary>
    /// 注册GameLuaObjectHost脚本到物体上
    /// </summary>
    /// <param name="name">GameLuaObjectHost脚本的名称</param>
    /// <param name="gameObject">要附加的物体</param>
    /// <param name="className">目标代码类名</param>
    /// <returns>返回新注册的 GameLuaObjectHost 实例</returns>
    [LuaApiDescription("注册GameLuaObjectHost脚本到物体上", "返回新注册的 GameLuaObjectHost 实例")]
    [LuaApiParamDescription("name", "GameLuaObjectHost脚本的名称")]
    [LuaApiParamDescription("gameObject", "要附加的物体")]
    [LuaApiParamDescription("className", "目标代码类名")]
    public GameLuaObjectHost RegisterLuaObject(string name, GameObject gameObject, string className)
    {
        GameLuaObjectHost newGameLuaObjectHost = gameObject.AddComponent<GameLuaObjectHost>();
        newGameLuaObjectHost.Name = name;
        newGameLuaObjectHost.Package = this;
        newGameLuaObjectHost.LuaState = PackageLuaState;
        newGameLuaObjectHost.LuaClassName = className;
        luaObjects.Add(newGameLuaObjectHost);
        return newGameLuaObjectHost;
    }
    /// <summary>
    /// 查找GameLuaObjectHost脚本
    /// </summary>
    /// <param name="name">GameLuaObjectHost脚本的名称</param>
    /// <param name="gameLuaObjectHost">输出GameLuaObjectHost脚本</param>
    /// <returns>返回是否找到对应脚本</returns>
    [LuaApiDescription("查找GameLuaObjectHost脚本", "返回是否找到对应脚本")]
    [LuaApiParamDescription("name", "GameLuaObjectHost脚本的名称")]
    [LuaApiParamDescription("gameLuaObjectHost", "输出GameLuaObjectHost脚本")]
    public bool FindLuaObject(string name, out GameLuaObjectHost gameLuaObjectHost)
    {
        foreach (GameLuaObjectHost luaObjectHost in luaObjects)
        {
            if (luaObjectHost.Name == name)
            {
                gameLuaObjectHost = luaObjectHost;
                return true;
            }
        }
        gameLuaObjectHost = null;
        return false;
    }
    //清除已释放的GameLuaObjectHost脚本
    internal void RemoveLuaObject(GameLuaObjectHost o)
    {
        if (luaObjects != null)
            luaObjects.Remove(o);
    }
    internal void AddeLuaObject(GameLuaObjectHost o)
    {
        if (luaObjects != null && !luaObjects.Contains(o))
            luaObjects.Add(o);
    }

    #endregion

    #endregion

    #region 模块信息

    private int VerConverter(string s)
    {
      if (s == "{internal.core.version}")
        return GameConst.GameBulidVersion;
      return ConverUtils.StringToInt(s, 0, "Package/version");
    }

    protected bool ReadInfo(XmlDocument xml)
    {
      XmlNode nodePackage = xml.SelectSingleNode("Package");
      XmlAttribute attributeName = nodePackage.Attributes["name"];
      XmlAttribute attributeVersion = nodePackage.Attributes["version"];
      XmlNode nodeBaseInfo = nodePackage.SelectSingleNode("BaseInfo");

      if (attributeName == null)
      {
        LoadError = "PackageDef.xml 配置存在错误 : name 丢失";
        GameErrorChecker.SetLastErrorAndLog(GameError.MissingAttribute, TAG, "Package attribute name is null");
        return false;
      }
      if (attributeVersion == null)
      {
        LoadError = "PackageDef.xml 配置存在错误 : version 丢失";
        GameErrorChecker.SetLastErrorAndLog(GameError.MissingAttribute, TAG, "Package attribute version is null");
        return false;
      }
      if (nodeBaseInfo == null)
      {
        LoadError = "PackageDef.xml 配置存在错误 : BaseInfo 丢失";
        GameErrorChecker.SetLastErrorAndLog(GameError.MissingAttribute, TAG, "Package node BaseInfo is null");
        return false;
      }

      //Version and PackageName
      PackageName = attributeName.Value;
      PackageVersion = VerConverter(attributeVersion.Value);

      //BaseInfo
      BaseInfo = new GamePackageBaseInfo(nodeBaseInfo, this);

      //Compatibility
      XmlNode nodeCompatibility = nodePackage.SelectSingleNode("Compatibility");
      if (nodeCompatibility != null)
        for (int i = 0; i < nodeCompatibility.Attributes.Count; i++)
        {
          switch (nodeCompatibility.ChildNodes[i].Name)
          {
            case "TargetVersion":
              TargetVersion = ConverUtils.StringToInt(nodeCompatibility.ChildNodes[i].InnerText,
                  GameConst.GameBulidVersion, "Compatibility/TargetVersion");
              break;
            case "MinVersion":
              MinVersion = ConverUtils.StringToInt(nodeCompatibility.ChildNodes[i].InnerText,
                  GameConst.GameBulidVersion, "Compatibility/MinVersion");
              break;
          }
        }

      //兼容性检查
      if (MinVersion > GameConst.GameBulidVersion)
      {
        Log.E(TAG, "MinVersion {0} greater than game version {1}", MinVersion, GameConst.GameBulidVersion);
        LoadError = "模块版本与当前游戏不兼容，模块所需版本 >=" + MinVersion;
        GameErrorChecker.LastError = GameError.PackageIncompatible;
        IsCompatible = false;
        return false;
      }
      else
      {
        IsCompatible = true;
      }

      //参数
      XmlNode nodeEntryCode = nodePackage.SelectSingleNode("EntryCode");
      if (nodeEntryCode != null)
        EntryCode = nodeEntryCode.InnerText;
      XmlNode nodeContainCSharp = nodePackage.SelectSingleNode("ContainCSharp");
      if (nodeContainCSharp != null)
        ContainCSharp = ConverUtils.StringToBoolean(nodeContainCSharp.InnerText, false, "ContainCSharp");
      XmlNode nodeType = nodePackage.SelectSingleNode("Type");
      if (nodeType != null)
        Type = ConverUtils.StringToEnum(nodeType.InnerText, GamePackageType.Asset, "Type");

      return true;
    }

    /// <summary>
    /// 获取模块文件路径
    /// </summary>
    [LuaApiDescription("获取模块文件路径")]
    public string PackageFilePath { get; protected set; }
    /// <summary>
    /// 获取模块包名
    /// </summary>
    [LuaApiDescription("获取模块包名")]
    public string PackageName { get; protected set; }
    /// <summary>
    /// 获取模块版本号
    /// </summary>
    [LuaApiDescription("获取模块版本号")]
    public int PackageVersion { get; protected set; }
    /// <summary>
    /// 获取基础信息
    /// </summary>
    [LuaApiDescription("获取基础信息")]
    public GamePackageBaseInfo BaseInfo { get; protected set; }
    /// <summary>
    /// 获取模块更新时间
    /// </summary>
    [LuaApiDescription("获取模块更新时间")]
    public DateTime UpdateTime { get; protected set; }
    /// <summary>
    /// 获取获取是否是系统必须包
    /// </summary>
    [LuaApiDescription("获取获取是否是系统必须包")]
    public bool SystemPackage { get; internal set; }

    /// <summary>
    /// 获取模块加载错误
    /// </summary>
    [LuaApiDescription("获取模块加载错误")]
    public string LoadError { get; protected set; } = "";

    /// <summary>
    /// 获取模块PackageDef文档
    /// </summary>
    [LuaApiDescription("获取模块PackageDef文档")]
    public XmlDocument PackageDef { get; protected set; }
    /// <summary>
    /// 获取模块AssetBundle
    /// </summary>
    [LuaApiDescription("获取模块AssetBundle")]
    public AssetBundle AssetBundle { get; protected set; }

    /// <summary>
    /// 获取表示模块目标游戏内核版本
    /// </summary>
    [LuaApiDescription("获取表示模块目标游戏内核版本")]
    public int TargetVersion { get; protected set; } = GameConst.GameBulidVersion;
    /// <summary>
    /// 获取表示模块可以正常使用的最低游戏内核版本
    /// </summary>
    [LuaApiDescription("获取表示模块可以正常使用的最低游戏内核版本")]
    public int MinVersion { get; protected set; } = GameConst.GameBulidVersion;
    /// <summary>
    /// 获取模块是否兼容当前内核
    /// </summary>
    [LuaApiDescription("获取模块是否兼容当前内核")]
    public bool IsCompatible { get; protected set; }

    /// <summary>
    /// 获取模块入口代码
    /// </summary>
    [LuaApiDescription("获取模块入口代码")]
    public string EntryCode { get; protected set; }
    /// <summary>
    /// 获取模块类型
    /// </summary>
    [LuaApiDescription("获取模块类型")]
    public GamePackageType Type { get; protected set; } = GamePackageType.Asset;
    /// <summary>
    /// 指示本模组是否要加载 CSharp 代码
    /// </summary>
    [LuaApiDescription("指示本模组是否要加载 CSharp 代码")]
    public bool ContainCSharp { get; protected set; } = false;

    internal GamePackageStatus _Status = GamePackageStatus.NotLoad;

    /// <summary>
    /// 获取模块加载状态
    /// </summary>
    [LuaApiDescription("获取模块加载状态")]
    public GamePackageStatus Status { get { return _Status; } }
    /// <summary>
    /// 转为字符串显示
    /// </summary>
    /// <returns></returns>
    [LuaApiNoDoc]
    public override string ToString()
    {
      return "Package: " + PackageName + "(" + PackageVersion + ") => " + _Status;
    }
    /// <summary>
    /// 展示所有资源
    /// </summary>
    /// <returns></returns>
    [LuaApiNoDoc]
    public virtual string ListResource()
    {
      StringBuilder sb = new StringBuilder();
      if(AssetBundle != null) {
        var list = AssetBundle.GetAllAssetNames();
        sb.Append("[AssetBundle " + AssetBundle.name + " assets count: " + list.Length + " ]");
        for (int i = 0; i < list.Length; i++)
          sb.AppendLine(list[i]);
      } else {
        sb.Append("[AssetBundle is null]");
      }
      return sb.ToString();
    }

    #endregion

    #region 资源读取

    /// <summary>
    /// 读取模块资源包中的资源
    /// </summary>
    /// <param name="pathorname">资源路径</param>
    /// <returns>返回资源实例，如果未找到，则返回null</returns>
    [LuaApiDescription("读取模块资源包中的资源", "返回资源实例，如果未找到，则返回null")]
    [LuaApiParamDescription("pathorname", "资源路径")]
    public virtual T GetAsset<T>(string pathorname) where T : UnityEngine.Object
    {
      if (AssetBundle == null)
      {
        GameErrorChecker.LastError = GameError.NotLoad;
        return null;
      }

      return AssetBundle.LoadAsset<T>(pathorname);
    }

    /// <summary>
    /// 读取模块资源包中的文字资源
    /// </summary>
    /// <param name="pathorname">资源路径</param>
    /// <returns>返回TextAsset实例，如果未找到，则返回null</returns>
    [LuaApiDescription("读取模块资源包中的文字资源", "返回TextAsset实例，如果未找到，则返回null")]
    [LuaApiParamDescription("pathorname", "资源路径")]
    public virtual TextAsset GetTextAsset(string pathorname) { return GetAsset<TextAsset>(pathorname); }

    /// <summary>
    /// 读取模块资源包中的 Prefab 资源
    /// </summary>
    /// <param name="pathorname">资源路径</param>
    /// <returns>返回 GameObject 实例，如果未找到，则返回null</returns>
    [LuaApiDescription("读取模块资源包中的 Prefab 资源", "返回 GameObject 实例，如果未找到，则返回null")]
    [LuaApiParamDescription("pathorname", "资源路径")]
    public virtual GameObject GetPrefabAsset(string pathorname) { return GetAsset<GameObject>(pathorname); }
    /// <summary>
    /// 读取模块资源包中的 Texture 资源
    /// </summary>
    /// <param name="pathorname">资源路径</param>
    /// <returns>返回 Texture 实例，如果未找到，则返回null</returns>
    [LuaApiDescription("读取模块资源包中的 Texture 资源", "返回资源实例，如果未找到，则返回null")]
    [LuaApiParamDescription("pathorname", "资源路径")]
    public virtual Texture GetTextureAsset(string pathorname) { return GetAsset<Texture>(pathorname); }
    /// <summary>
    /// 读取模块资源包中的 Texture2D 资源
    /// </summary>
    /// <param name="pathorname">资源路径</param>
    /// <returns>返回 Texture2D 实例，如果未找到，则返回null</returns>
    [LuaApiDescription("读取模块资源包中的 Texture2D 资源", "返回资源实例，如果未找到，则返回null")]
    [LuaApiParamDescription("pathorname", "资源路径")]
    public virtual Texture2D GetTexture2DAsset(string pathorname) { return GetAsset<Texture2D>(pathorname); }
    /// <summary>
    /// 读取模块资源包中的 Sprite 资源
    /// </summary>
    /// <param name="pathorname">资源路径</param>
    /// <returns>返回 Sprite 实例，如果未找到，则返回null</returns>
    [LuaApiDescription("读取模块资源包中的 Sprite 资源", "返回资源实例，如果未找到，则返回null")]
    [LuaApiParamDescription("pathorname", "资源路径")]
    public virtual Sprite GetSpriteAsset(string pathorname) { return GetAsset<Sprite>(pathorname); }
    /// <summary>
    /// 读取模块资源包中的 Material 资源
    /// </summary>
    /// <param name="pathorname">资源路径</param>
    /// <returns>返回 Material 实例，如果未找到，则返回null</returns>
    [LuaApiDescription("读取模块资源包中的 Material 资源", "返回资源实例，如果未找到，则返回null")]
    [LuaApiParamDescription("pathorname", "资源路径")]
    public virtual Material GetMaterialAsset(string pathorname) { return GetAsset<Material>(pathorname); }
    /// <summary>
    /// 读取模块资源包中的 PhysicMaterial 资源
    /// </summary>
    /// <param name="pathorname">资源路径</param>
    /// <returns>返回 PhysicMaterial 实例，如果未找到，则返回null</returns>
    [LuaApiDescription("读取模块资源包中的 PhysicMaterial 资源", "返回资源实例，如果未找到，则返回null")]
    [LuaApiParamDescription("pathorname", "资源路径")]
    public virtual PhysicMaterial GetPhysicMaterialAsset(string pathorname) { return GetAsset<PhysicMaterial>(pathorname); }
    /// <summary>
    /// 读取模块资源包中的 AudioClip 资源
    /// </summary>
    /// <param name="pathorname">资源路径</param>
    /// <returns>返回 AudioClip 实例，如果未找到，则返回null</returns>
    [LuaApiDescription("读取模块资源包中的 AudioClip 资源", "返回资源实例，如果未找到，则返回null")]
    [LuaApiParamDescription("pathorname", "资源路径")]
    public virtual AudioClip GetAudioClipAsset(string pathorname) { return GetAsset<AudioClip>(pathorname); }

    /// <summary>
    /// 读取模块资源包中的代码资源
    /// </summary>
    /// <param name="pathorname">文件名称或路径</param>
    /// <returns>如果读取成功则返回代码内容，否则返回null</returns>
    [LuaApiDescription("读取模块资源包中的Lua代码资源", "如果读取成功则返回代码内容，否则返回null")]
    [LuaApiParamDescription("pathorname", "文件名称或路径")]
    public virtual CodeAsset GetCodeAsset(string pathorname)
    {
      TextAsset textAsset = GetTextAsset(pathorname);
      if (textAsset != null)
        return new CodeAsset(textAsset.bytes, pathorname, pathorname, pathorname);

      GameErrorChecker.LastError = GameError.FileNotFound;
      return null;
    }
    /// <summary>
    /// 加载模块资源包中的c#代码资源
    /// </summary>
    /// <param name="pathorname">资源路径</param>
    /// <returns>如果加载成功则返回已加载的Assembly，否则将抛出异常，若当前环境并不支持加载，则返回null</returns>
    [LuaApiDescription("加载模块资源包中的c#代码资源", "如果加载成功则返回已加载的Assembly，否则将抛出异常，若当前环境并不支持加载，则返回null")]
    [LuaApiParamDescription("pathorname", "资源路径")]
    public virtual Assembly LoadCodeCSharp(string pathorname)
    {
      GameErrorChecker.SetLastErrorAndLog(GameError.NotSupportFileType, TAG, "当前模块不支持加载 CSharp 代码");
      return null;
    }

    /// <summary>
    /// 表示代码资源
    /// </summary>
    [LuaApiDescription("表示代码资源")]
    [SLua.CustomLuaClass]
    [LuaApiNoDoc]
    public class CodeAsset {
      /// <summary>
      /// 代码字符串
      /// </summary>
      [LuaApiDescription("代码字符串")]
      public byte[] data;
      /// <summary>
      /// 获取当前代码的真实路径（一般用于调试）
      /// </summary>
      [LuaApiDescription("获取当前代码的真实路径（一般用于调试）")]
      public string realPath;
      /// <summary>
      /// 代码文件的相对路径
      /// </summary>
      [LuaApiDescription("代码文件的相对路径")]
      public string relativePath;
      /// <summary>
      /// 调试器中显示的路径
      /// </summary>
      [LuaApiDescription("调试器中显示的路径")]
      public string debugPath;

      public CodeAsset(byte[] data, string realPath, string relativePath, string debugPath) {
        this.data = data;
        this.realPath = realPath;
        this.relativePath = relativePath;
        this.debugPath = debugPath;
      }

      /// <summary>
      /// 获取代码字符串
      /// </summary>
      /// <returns></returns>
      [LuaApiDescription("获取代码字符串")]
      public string GetCodeString() {
        return Encoding.UTF8.GetString(StringUtils.FixUtf8BOM(data));
      }
    }

    #endregion

    #region 模块操作

    /// <summary>
    /// 修复 模块透明材质 Shader
    /// </summary>
    private void FixBundleShader()
    {
      if (AssetBundle == null)
        return;
#if UNITY_EDITOR //editor 模式下修复一下透明shader
      int _SrcBlend = 0;
      int _DstBlend = 0;

      var materials = AssetBundle.LoadAllAssets<Material>();
      var standardShader = Shader.Find("Standard");
      if (standardShader == null)
        return;
      foreach (Material material in materials)
      {
        var shaderName = material.shader.name;
        if (shaderName == "Standard")
        {
          material.shader = standardShader;

          _SrcBlend = material.renderQueue == 0 ? 0 : material.GetInt("_SrcBlend");
          _DstBlend = material.renderQueue == 0 ? 0 : material.GetInt("_DstBlend");

          if (_SrcBlend == (int)UnityEngine.Rendering.BlendMode.SrcAlpha
              && _DstBlend == (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha)
          {
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
          }
        }
      } 
#elif UNITY_STANDALONE
      var materials = AssetBundle.LoadAllAssets<Material>();
      var transparentShader = Shader.Find("Custom/TransparentShader");
      if (transparentShader == null) {
        Log.D(TAG, "Not found Custom/TransparentShader");
        return;
      }
      foreach (Material material in materials)
      {
        var shaderName = material.shader.name;
        if (shaderName == "Custom/TransparentShader")
        {
          material.shader = transparentShader;
          Log.D(TAG, "Fix Shader for {0}", material.name);
        }
      }
#endif

    }

    private Dictionary<string, string> preI18NResource = new Dictionary<string, string>();

    /// <summary>
    /// 在当前模块中预加载的国际化语言资源寻找字符串
    /// </summary>
    /// <param name="key">键</param>
    /// <returns>返回国际化字符串，如果未找到，则返回null</returns>
    [LuaApiDescription("在当前模块中预加载的国际化语言资源寻找字符串", "返回国际化字符串，如果未找到，则返回null")]
    [LuaApiParamDescription("key", "键")]
    public string GetPackageI18NResourceInPre(string key) {
      if(preI18NResource.TryGetValue(key, out var s))
        return s;
      return null;
    }

    /// <summary>
    /// 预加载国际化语言资源
    /// </summary>
    protected void PreLoadI18NResource(string resString) {
      if (resString != null)
        preI18NResource = I18NProvider.PreLoadLanguageResources(resString);
      else {
        var res = GetTextAsset("PackageLanguageResPre.xml");
        if (res != null)
          preI18NResource = I18NProvider.PreLoadLanguageResources(res.text);
      }
    }
    /// <summary>
    /// 加载模块的国际化语言资源
    /// </summary>
    private void LoadI18NResource()
    {
      var res = GetTextAsset("PackageLanguageRes.xml");
      if (res != null)
      {
        if (!I18NProvider.LoadLanguageResources(res.text))
          Log.E(TAG, "Failed to load PackageLanguageRes.xml for package " + PackageName);
      }
    }

    //自定义数据,方便LUA层操作

    private Dictionary<string, object> packageCustomData = new Dictionary<string, object>();

    /// <summary>
    /// 添加自定义数据
    /// </summary>
    /// <param name="name">数据名称</param>
    /// <param name="data">数据值</param>
    /// <returns>返回数据值</returns>
    [LuaApiDescription("添加自定义数据", "返回数据值")]
    [LuaApiParamDescription("name", "数据名称")]
    [LuaApiParamDescription("data", "数据值")]
    public object AddCustomProp(string name, object data)
    {
      if (packageCustomData.ContainsKey(name))
      {
        packageCustomData[name] = data;
        return data;
      }
      packageCustomData.Add(name, data);
      return data;
    }
    /// <summary>
    /// 获取自定义数据
    /// </summary>
    /// <param name="name">数据名称</param>
    /// <returns>返回数据值</returns>
    [LuaApiDescription("获取自定义数据", "返回数据值")]
    [LuaApiParamDescription("name", "数据名称")]
    [LuaApiParamDescription("data", "数据值")]
    public object GetCustomProp(string name)
    {
      if (packageCustomData.ContainsKey(name))
        return packageCustomData[name];
      return null;
    }
    /// <summary>
    /// 设置自定义数据
    /// </summary>
    /// <param name="name">数据名称</param>
    /// <param name="data"></param>
    /// <returns>返回旧的数据值，如果之前没有该数据，则返回null</returns>
    [LuaApiDescription("设置自定义数据", "返回旧的数据值，如果之前没有该数据，则返回null")]
    [LuaApiParamDescription("name", "数据名称")]
    [LuaApiParamDescription("data", "数据值")]
    public object SetCustomProp(string name, object data)
    {
      if (packageCustomData.ContainsKey(name))
      {
        object old = packageCustomData[name];
        packageCustomData[name] = data;
        return old;
      }
      return null;
    }
    /// <summary>
    /// 清除自定义数据
    /// </summary>
    /// <param name="name">数据名称</param>
    /// <returns>返回是否成功</returns>
    [LuaApiDescription("清除自定义数据", "返回是否成功")]
    [LuaApiParamDescription("name", "数据名称")]
    public bool RemoveCustomProp(string name)
    {
      if (packageCustomData.ContainsKey(name))
      {
        packageCustomData.Remove(name);
        return true;
      }
      return false;
    }

    #endregion

    #region 模块从属资源处理

    private HashSet<GameHandler> packageHandlers = new HashSet<GameHandler>();
  
    internal void HandlerReg(GameHandler handler)
    {
      packageHandlers.Add(handler);
    }
    internal void HandlerRemove(GameHandler handler)
    {
      packageHandlers.Remove(handler);
    }

    //释放所有从属于当前模块的GameHandler
    private void HandlerClear()
    {
      List<GameHandler> list = new List<GameHandler>(packageHandlers);
      foreach (GameHandler gameHandler in list)
        gameHandler.Dispose();
      list.Clear();
      packageHandlers.Clear();
    }

    #endregion

  }
}
