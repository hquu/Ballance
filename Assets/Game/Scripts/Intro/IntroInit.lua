--[[
Copyright(c) 2021  mengyu
* 模块名：     
Entry.lua
* 用途：
游戏进入动画入口
* 作者：
mengyu
]]--

local GameManager = Ballance2.Services.GameManager
local GameEventNames = Ballance2.Base.GameEventNames
local GameUIManager = GameManager.GetSystemService('GameUIManager') ---@type GameUIManager
local GameSoundManager = GameManager.GetSystemService('GameSoundManager') ---@type GameSoundManager
local GameSoundType = Ballance2.Services.GameSoundType
local GamePackage = Ballance2.Package.GamePackage
local Log = Ballance2.Log
local Yield = UnityEngine.Yield
local WaitForSeconds = UnityEngine.WaitForSeconds

local IntroUI = nil
local TAG = 'Intro:Entry'

---进入Intro场景
---@param thisGamePackage GamePackage
local function OnEnterIntro(thisGamePackage)

  Log.D(TAG, 'Into intro ui')

  if IntroUI == nil then
    IntroUI = GameUIManager:InitViewToCanvas(thisGamePackage:GetPrefabAsset('IntroUI.prefab'), 'IntroUI', true)
    IntroUI:SetAsFirstSibling()    
  end

  GameUIManager:MaskBlackFadeIn(0.3)

  --进入音乐
  GameSoundManager:PlayFastVoice("core.sounds.music:Music_Theme_4_1.wav", GameSoundType.Background)

  --延时5s
  coroutine.resume(coroutine.create(function()
    Yield(WaitForSeconds(5))
 
    --黑色渐变进入
    GameUIManager:MaskBlackFadeIn(1)
    Yield(WaitForSeconds(1))

    --进入菜单
    GameManager.Instance:RequestEnterLogicScense('MenuLevel')
  end))
end
local function OnQuitIntro()
  Log.D(TAG, 'Quit intro ui')
  if IntroUI ~= nil then
    IntroUI.gameObject:SetActive(false)
  end
end

return {
  Init = function ()
    local thisGamePackage = GamePackage.GetCorePackage()
    GameManager.GameMediator:RegisterEventHandler(thisGamePackage, GameEventNames.EVENT_LOGIC_SECNSE_ENTER, "Intro", function (evtName, params)
      local scense = params[1]
      if(scense == 'Intro') then OnEnterIntro(thisGamePackage) end
      return false
    end)    
    GameManager.GameMediator:RegisterEventHandler(thisGamePackage, GameEventNames.EVENT_LOGIC_SECNSE_QUIT, "Intro", function (evtName, params)
      local scense = params[1]
      if(scense == 'Intro') then OnQuitIntro() end
      return false
    end)
  end,
  Unload = function ()
    if (not Slua.IsNull(IntroUI)) then UnityEngine.Object.Destroy(IntroUI.gameObject) end 
    return true
  end
}