---@gendoc
---@gendoc
--[[
---@gen_remark_start

---@gen_remark_end
]]--

---球声音管理器，负责管理球的滚动碰撞声音。
---@class BallSoundManager : GameLuaObjectHostClass
BallSoundManager = ClassicObject:extend()

---球声音碰撞信息
---@class BallSoundCollData 
---@field MinSpeed number 碰撞检测最低速度
---@field MaxSpeed number 碰撞检测最高速度
---@field SleepAfterwards number 碰撞检测延时
---@field SpeedThreadhold number 碰撞检测最低速度阈值
---@field TimeDelayStart number 滚动检测起始延时
---@field TimeDelayEnd number 滚动检测末尾延时
---@field HasRollSound boolean 是否有滚动声音
---@field RollSoundName string 滚动声音名称，放在球的RollSound信息中
---@field HitSoundName string 撞击声音名称，放在球的HitSound信息中
BallSoundCollData = {}

local TAG = 'BallSoundManager'
local GamePackage = Ballance2.Package.GamePackage
local Log = Ballance2.Log

function BallSoundManager:new() 
  GamePlay.BallSoundManager = self
  self._MyEventHandlers = {}

  self._CurrentPlayingRollSounds = {}
  self._CurrentCollId = 1
  self._CollIDNames = {}
  self._SoundCollData = {} ---@type BallSoundCollData[]
end

--加载与卸载

function BallSoundManager:Start() 
  local sysPackage = GamePackage.GetCorePackage()
  self._MyEventHandlers[0] = Game.Mediator:RegisterEventHandler(sysPackage, "EVENT_LEVEL_BUILDER_START", TAG, function ()
    self:_AddInternalSoundCollData()
    return false
  end)
  self._MyEventHandlers[1] = Game.Mediator:RegisterEventHandler(sysPackage, "EVENT_LEVEL_BUILDER_UNLOAD_START", TAG, function ()
    self:RemoveAllSoundCollData()
    return false
  end)
end
function BallSoundManager:OnDestroy() 
  Game.Mediator:UnRegisterEventHandler("EVENT_LEVEL_BUILDER_START", self._MyEventHandlers[0])
  Game.Mediator:UnRegisterEventHandler("EVENT_LEVEL_BUILDER_UNLOAD_START", self._MyEventHandlers[1])
end

--内部函数

function BallSoundManager:_AddInternalSoundCollData() 
  self:AddSoundCollData(self:GetSoundCollIDByName('Stone'), {
    MinSpeed = 5,
    MaxSpeed = 30,
    SleepAfterwards = 0.6,
    SpeedThreadhold = 20,
    HasRollSound = true,
    RollSoundName = 'Stone',
    HitSoundName = 'Stone'
  })
  self:AddSoundCollData(self:GetSoundCollIDByName('Wood'), {
    MinSpeed = 5,
    MaxSpeed = 30,
    SleepAfterwards = 0.6,
    SpeedThreadhold = 20,
    HasRollSound = true,
    RollSoundName = 'Wood',
    HitSoundName = 'Wood'
  })
  self:AddSoundCollData(self:GetSoundCollIDByName('Metal'), {
    MinSpeed = 5,
    MaxSpeed = 30,
    SleepAfterwards = 0.6,
    SpeedThreadhold = 20,
    HasRollSound = true,
    RollSoundName = 'Metal',
    HitSoundName = 'Metal'
  })
end
function BallSoundManager:RemoveAllSoundCollData() 
  for index, value in pairs(self._SoundCollData) do
    if value ~= nil then
      self:RemoveSoundCollData(index)
    end
  end
end

---添加球碰撞声音组
---@param colId number 自定义碰撞层ID，为防止重复，请使用 GetSoundCollIDByName 使用名称获取ID
---@param data BallSoundCollData 碰撞数据
function BallSoundManager:AddSoundCollData(colId, data) 
  if self._SoundCollData[colId] ~= nil then
    Log.E(TAG, 'AddSoundCollData failed because SoundCollData id: '..colId..' already added')
    return
  end
  self._SoundCollData[colId] = data
end
---移除球碰撞声音组
---（注意，不会移除激活中球的声音组，需要等到球下一次激活时生效，因此不建议在游戏中使用此函数）
---@param colId number 自定义碰撞层ID
function BallSoundManager:RemoveSoundCollData(colId) 
  self._SoundCollData[colId] = nil
end
---通过名称分配一个可用的声音组ID, 如果名称存在，则返回同样的ID
---@param name string 自定义声音组名称
function BallSoundManager:GetSoundCollIDByName(name) 
  if type(self._CollIDNames[name]) == 'number' then
    return self._CollIDNames[name]
  else
    local id = self._CurrentCollId
    self._CurrentCollId = self._CurrentCollId + 1
    self._CollIDNames[name] = id
    return id
  end
end

---添加球的声音处理函数(由BallManager调用)
---@param ball BallRegStorage
function BallSoundManager:AddSoundableBall(ball) 
  Log.D(TAG, 'AddSoundableBall '..ball.name)
  --添加声音层工具侦听
  for id, value in pairs(self._SoundCollData) do
    if value ~= nil then
      ball.rigidbody:AddCollDetection(id, value.MinSpeed, value.MaxSpeed, value.SleepAfterwards, value.SpeedThreadhold)
      if value.HasRollSound then
        ball.rigidbody:AddContractDetection(id, ball.ball._RollSound.TimeDelayStart, ball.ball._RollSound.TimeDelayEnd)
      end
    end
  end
  --添加回调
  ball.rigidbody:EnableCollisionDetection()
  ball.rigidbody:EnableContractEventCallback()
  ---撞击处理回调
  ---@param col_id number
  ---@param speed_precent number
  ball.rigidbody.OnPhysicsCollDetection = function (_, col_id, speed_precent)
    local data = self._SoundCollData[col_id]
    if data then
      local sound = ball.ball._HitSound.Sounds[data.HitSoundName] or ball.ball._HitSound.Sounds.All
      if sound then
        --这里是切换了两个sound的播放，因为碰撞声音很可能
        --一个没有播放完成另一个就来了
        if sound.isSound1 then
          sound.isSound1 = false
          sound.sound1.volume = speed_precent
          if not sound.sound1.isPlaying then
            sound.sound1:Play() 
          end
        else
          sound.isSound1 = true
          sound.sound2.volume = speed_precent
          if not sound.sound2.isPlaying then
            sound.sound2:Play() 
          end
        end
      end
    end
  end
  ---接触开始处理回调
  ---@param col_id number
  ball.rigidbody.OnPhysicsContactOn = function (_, col_id)
    
    --print('ball '..ball.name..' OnPhysicsContactOn: '..tostring(col_id));
    local data = self._SoundCollData[col_id]
    if data and data.HasRollSound then
      local sound = ball.ball._RollSound.Sounds[data.RollSoundName] or ball.ball._RollSound.Sounds.All
      if sound then
        --加入正在播放声音中
        self._CurrentPlayingRollSounds[col_id] = sound
      end
    end
  end
  ---接触结束处理回调
  ---@param col_id number
  ball.rigidbody.OnPhysicsContactOff = function (_, col_id)
    local data = self._SoundCollData[col_id]
    --print('ball '..ball.name..' OnPhysicsContactOff: '..tostring(col_id));
    if data and data.HasRollSound then
      local sound = ball.ball._RollSound.Sounds[data.RollSoundName] or ball.ball._RollSound.Sounds.All
      if sound then
        --从正在播放声音中移除
        self._CurrentPlayingRollSounds[col_id] = nil
        sound.volume = 0
      end
    end
  end
end
---移除球的声音处理函数(由BallManager调用)
---@param ball BallRegStorage
function BallSoundManager:RemoveSoundableBall(ball) 
  Log.D(TAG, 'RemoveSoundableBall '..ball.name)
  --移除回调
  ball.rigidbody.OnPhysicsCollDetection = nil
  ball.rigidbody.OnPhysicsContactOn = nil
  ball.rigidbody.OnPhysicsContactOff = nil
  ball.rigidbody:DeleteAllCollDetection()
  ball.rigidbody:DeleteAllContractDetection()
  ball.rigidbody:DisableCollisionDetection()
  --移除声音层工具侦听
  for id, _ in pairs(self._SoundCollData) do
    local sound = self._CurrentPlayingRollSounds[id]
    if sound ~= nil then
      self._CurrentPlayingRollSounds[id] = nil
      sound.volume = 0
      sound.pitch = 0
    end
  end
end

---滚动声音音量与速度处理(由BallManager调用)
---@param ball Ball
---@param speedMeter SpeedMeter
function BallSoundManager:HandlerBallRollSpeedChange(ball, speedMeter)
  local _RollSound = ball._RollSound
  local speed = speedMeter.NowAbsoluteSpeed
  local vol = _RollSound.VolumeBase + (speed * _RollSound.VolumeFactor)
  local pit = _RollSound.PitchBase + (speed * _RollSound.PitchFactor)

  if vol > 1 then vol = 1 end
  if pit > 1 then pit = 1 end

  --print('speed '..tostring(speed)..' count: '..(#self._CurrentPlayingRollSounds))

  if BALLANCE_DEBUG then
    ball._SoundManagerDebugStrings = ''
  end

  --将音量设置到正在播放的声音中
  for id, value in pairs(self._CurrentPlayingRollSounds) do
    if value then

      if BALLANCE_DEBUG then
        ball._SoundManagerDebugStrings = ball._SoundManagerDebugStrings..string.format('\n> [%d %s] %.2f %.2f', id, value.name, vol, pit)
      end

      value.volume = vol
      value.pitch = pit
    end
  end
end

---强制停止所有球声音
function BallSoundManager:StopAllSound()
  Log.D(TAG, 'StopAllSound')
  for _, value in pairs(self._CurrentPlayingRollSounds) do
    if value then
      value.volume = 0
      value.pitch = 1
    end
  end
end

function CreateClass:BallSoundManager() 
  return BallSoundManager()
end