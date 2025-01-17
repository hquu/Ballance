local Vector3 = UnityEngine.Vector3
local Time = UnityEngine.Time
local FogMode = UnityEngine.FogMode
local RenderSettings = UnityEngine.RenderSettings
local Color = UnityEngine.Color
local GameManager = Ballance2.Services.GameManager
local GameUIManager = GameManager.GetSystemService("GameUIManager") ---@type GameUIManager
local GameSoundManager = GameManager.GetSystemService("GameSoundManager") ---@type GameSoundManager
local GameSoundType = Ballance2.Services.GameSoundType
local SkyBoxUtils = Ballance2.Game.Utils.SkyBoxUtils

---Menu level 摄像机控制类
---@class CameraControl : GameLuaObjectHostClass
local CameraControl = {
  I_Zone = nil, ---@type GameObject
  I_Zone_SuDu = nil, ---@type GameObject
  I_Zone_NenLi = nil, ---@type GameObject
  I_Zone_LiLiang = nil, ---@type GameObject
  I_Dome = nil, ---@type GameObject
  skyBox = nil, ---@type Skybox
  skyBoxNight = nil, ---@type Material
  skyBoxDay = nil, ---@type Material
  menuSound = nil,
  speed = -10,
  state = {
    isInLightZone = false,
    isRoatateCam = true,
  },
  domePosition = nil,
  transformI_Zone_SuDu = nil,
  transformI_Zone_NenLi = nil,
  transformI_Zone_LiLiang = nil,
}

EVENT_SWITCH_LIGHTZONE = 'swicth_menulevel_lightzone'

function CreateClass:CameraControl()
  
  function CameraControl:new(o)
    o = o or {}
    setmetatable(o, self)
    self.__index = self
    return o
  end

  function CameraControl:Start()
    self.domePosition = self.I_Dome.transform.position
    self.transformI_Zone_SuDu = self.I_Zone_SuDu.transform
    self.transformI_Zone_NenLi = self.I_Zone_NenLi.transform
    self.transformI_Zone_LiLiang = self.I_Zone_LiLiang.transform
    self.skyBoxNight = SkyBoxUtils.MakeSkyBox('M')
    self.skyBoxDay = SkyBoxUtils.MakeSkyBox('C')
    self.menuSound = GameSoundManager:RegisterSoundPlayer(GameSoundType.Background, GameSoundManager:LoadAudioResource('core.sounds.music:Menu_atmo.wav'), false, true, 'MenuSound')
    self.menuSound:Play()
    self.menuSoundRandomTimer = nil

    --随机时间播放Menu_atmo
    local startRandomMenuSound = nil
    startRandomMenuSound = function ()
      self.menuSoundRandomTimer = LuaTimer.Add(self.menuSound.clip.length * 1000, function ()
        self.menuSoundRandomTimer = LuaTimer.Add(math.random(1, 10) * 1000, function ()
          self.menuSound:Play()
          startRandomMenuSound()
        end)
      end)
    end
    startRandomMenuSound()

    self:SwitchLightZone(false, false)
    self._Stared = true

    GameManager.GameMediator:RegisterSingleEvent(EVENT_SWITCH_LIGHTZONE)
    GameManager.GameMediator:SubscribeSingleEvent(self.package, EVENT_SWITCH_LIGHTZONE, "CameraControl", function (evtName, params)
      if (params[1]) then
        self:SwitchLightZone(true, true)
      else
        self:SwitchLightZone(false, true)
      end
      return false
    end)
  end
  function CameraControl:Update()
    if(self.state.isRoatateCam) then
			self.transform:RotateAround(self.domePosition, Vector3.up, Time.deltaTime * self.speed)
			self.transform:LookAt(self.domePosition)
    end
    if(self.state.isInLightZone) then 
      self.transformI_Zone_SuDu:LookAt(self.transform.position, Vector3.up)
      self.transformI_Zone_NenLi:LookAt(self.transform.position, Vector3.up)
      self.transformI_Zone_LiLiang:LookAt(self.transform.position, Vector3.up)
      self.transformI_Zone_SuDu.eulerAngles = Vector3(0, self.transformI_Zone_SuDu.eulerAngles.y, 0)
      self.transformI_Zone_NenLi.eulerAngles = Vector3(0, self.transformI_Zone_NenLi.eulerAngles.y, 0)
      self.transformI_Zone_LiLiang.eulerAngles = Vector3(0, self.transformI_Zone_LiLiang.eulerAngles.y, 0)
    end
  end
  function CameraControl:OnDisable()
    self.menuSound:Stop()

    if self.menuSoundRandomTimer then
      LuaTimer.Delete(self.menuSoundRandomTimer)
      self.menuSoundRandomTimer = nil
    end
  end
  function CameraControl:OnEnable()
    if self._Stared then 
      self.menuSound.loop = true
      self.menuSound:Play()
      self:SwitchLightZone(false, false)
    end
  end

  function CameraControl:SetFog(isLz) 
    RenderSettings.fog = true
    RenderSettings.fogDensity = 0.03
    RenderSettings.fogStartDistance = 100
    RenderSettings.fogEndDistance = 800
    RenderSettings.fogMode = FogMode.Linear
    if(isLz) then
      RenderSettings.fogColor = Color(0.180, 0.254, 0.301)
    else
      RenderSettings.fogColor = Color(0.827, 0.784, 0.581)
    end
  end
  ---切换主菜单关卡LightZone模式
  ---@param on boolean 是否是LightZone模式
  ---@param isClick boolean 是否是用户点击所触发的
  function CameraControl:SwitchLightZone(on, isClick) 
    if(on) 
    then
      GameSoundManager:PlayFastVoice('core.sounds.music:Music_thunder.wav', GameSoundType.Background)
      GameUIManager:MaskBlackSet(true)
      GameUIManager:MaskBlackFadeOut(1)
      GameManager.GameLight.color = Color(0.2,0.4,0.6)
      self.I_Zone:SetActive(true)
      self.skyBox.material = self.skyBoxNight
      self.state.isInLightZone = true
      self:SetFog(true)
    else
      if isClick then
        GameUIManager:MaskBlackSet(true)
        GameUIManager:MaskBlackFadeOut(1)
      end
      GameManager.GameLight.color = Color(1,1,1)
      self.I_Zone:SetActive(false)
      self.skyBox.material = self.skyBoxDay
      self.state.isInLightZone = false
      self:SetFog(false)
    end
  end

  return CameraControl:new(nil)
end
