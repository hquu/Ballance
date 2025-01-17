local Time = UnityEngine.Time
local Color = UnityEngine.Color

local GamePackage = Ballance2.Package.GamePackage
local GameSoundType = Ballance2.Services.GameSoundType

---闪电控制管理器
---@class LevelBriz : GameLuaObjectHostClass
---@field _BrizCurve AnimationCurve
---@field _BrizLight Light
---@field _BrizTime number
LevelBriz = ClassicObject:extend()

function LevelBriz:new()
  self._LightEnable = false
  self._LightTick = math.random(10, 90)
  self._LightFlash = false
  self._LightFlashTick = 10
end
function LevelBriz:Start()
  self._BrizLight.color = Color(0,0,0,1)
  --注册模组自定义入口
  Game.Mediator:RegisterEventHandler(GamePackage.GetCorePackage(), 'CoreBrizLevelEventHandler', 'LevelBrizHandler', function (evtName, params)
    if params[1] == 'beforeStart' then
      self._LightEnable = true
    elseif params[1] == 'beforeQuit' then
      self._LightEnable = false
    end
    return false
  end)
end
function LevelBriz:Update()
  if self._LightFlash then
    self._LightFlashTick = self._LightFlashTick + Time.deltaTime
    local v = self._BrizCurve:Evaluate(self._LightFlashTick / self._BrizTime)
    if v > 1 then
      self._LightFlash = false
    else
      self._BrizLight.color = Color(v, v, v, 1)
    end
  end
end
function LevelBriz:FixedUpdate()
  if self._LightEnable then
    if self._LightTick > 0 then
      self._LightTick = self._LightTick - 1
    else
      self:LightFlash()
      self._LightTick = math.random(10, 90)
    end
  end
end
function LevelBriz:LightFlash()
  self._LightFlash = true
  self._LightFlashTick = 0
  Game.SoundManager:PlayFastVoice('core.sounds.music:Music_thunder.wav', GameSoundType.Normal)
end

function CreateClass:LevelBriz()
  return LevelBriz()
end