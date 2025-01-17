---@gendoc

local Vector3 = UnityEngine.Vector3
local Quaternion = UnityEngine.Quaternion
local Time = UnityEngine.Time
local CamFollow = Ballance2.Game.CamFollow

---摄像机管理器，负责游戏中的摄像机运动。
---@class CamManager : GameLuaObjectHostClass
---@field _CamOrient GameObject
---@field _CamTarget Transform
---@field _CamOrientTransform Transform
---@field _SkyBox Skybox
---@field _CamRotateSpeedCurve AnimationCurve 
---@field _CamUpSpeedCurve AnimationCurve 
---@field _CameraRotateTime number
---@field _CameraRotateUpTime number
---@field _CameraNormalZ number
---@field _CameraNormalY number
---@field _CameraSpaceY number
---@field _CamPosFrame Transform 
---@field CamDirectionRef Transform 获取球参照的摄像机旋转方向变换 [R]
---@field CamRightVector Vector3 获取摄像机右侧向量 [R]
---@field CamLeftVector Vector3 获取摄像机左侧向量 [R]
---@field CamForwerdVector Vector3 获取摄像机向前向量 [R]
---@field CamBackVector Vector3 获取摄像机向后向量 [R]
---@field CamFollowSpeed number 摄像机跟随速度 [RW]
---@field CamIsSpaced boolean 获取摄像机是否空格键升高了 [R]
---@field CamRotateValue number 获取当前摄像机方向 [R] 设置请使用 RotateTo 方法
---@field CamFollow CamFollow 获取摄像机跟随脚本 [R]
CamManager = ClassicObject:extend()

function CamManager:new()
  self._CameraRotateTime = 0.4
  self._CameraRotateUpTime = 0.8
  self._CameraNormalZ = 17
  self._CameraNormalY = 30
  self._CameraSpaceY = 55
  self._CameraSpaceZ = 8
  self.CamRightVector = Vector3.right
  self.CamLeftVector = Vector3.left
  self.CamForwerdVector = Vector3.forward
  self.CamBackVector = Vector3.back
  self.CamDirectionRef = nil
  self.CamFollowSpeed = 0.05
  self.CamIsSpaced = false
  self.Target = nil
  self.CamRotateValue = 0
  self._CamRotateValueNow = 0

  self._CamIsRotateing = false
  self._CamRotateTick = 0
  self._CamIsRotateingUp = false
  self._CamRotateUpTick = 0
  self._CamRotateUpStart = { 
    z = 0,
    y = 0
  }
  self._CamRotateUpTarget = { 
    z = 0,
    y = 0
  }
  self._CamOutSpeed = Vector3.zero
  self._CamRotateStartDegree = 0
  self._CamRotateTargetDegree = 0

  GamePlay.CamManager = self
end

function CamManager:Start()
  self._CamPosFrame.localPosition = Vector3(0, self._CameraNormalY, -self._CameraNormalZ)
  self.transform.position = self._CamPosFrame.position;
  self.transform:LookAt(self._CamTarget)
  self.CamDirectionRef = self._CamOrient.transform

  --注册事件
  local events = Game.Mediator:RegisterEventEmitter('CamManager')
  self.EventRotateUpStateChanged = events:RegisterEvent('RotateUpStateChanged') --空格键升起摄像机状态变化事件
  self.EventRotateDirectionChanged = events:RegisterEvent('RotateDirectionChanged') --摄像机旋转方向变化事件
  self.EventCamFollowChanged = events:RegisterEvent('CamFollowChanged') --摄像机是否跟踪目标变化事件
  self.EventCamLookChanged = events:RegisterEvent('CamLookChanged') --摄像机对准目标变化事件
  self.EventCamFollowTargetChanged = events:RegisterEvent('CamFollowTargetChanged') --摄像机跟踪目标变化事件

  self._CommandId = Game.Manager.GameDebugCommandServer:RegisterCommand('cam', function (eyword, fullCmd, argsCount, args)
    local type = args[1]
    if type == 'left' then
      self:RotateLeft()
    elseif type == 'right' then
      self:RotateLeft()
    elseif type == 'up' then
      self:RotateUp(true)
    elseif type == 'down' then
      self:RotateUp(false)
    elseif type == 'follow' then
      self:SetCamFollow(true)
    elseif type == 'no-follow' then
      self:SetCamFollow(false)
    elseif type == 'look' then
      self:SetCamLook(true)
    elseif type == 'no-look' then
      self:SetCamLook(false)
    end
    return true
  end, 1, "cam <left/right/up/down/-all> 摄像机管理器命令"..
          "  left      ▶ 向左旋转摄像机"..
          "  right     ▶ 向右旋转摄像机"..
          "  up        ▶ 空格键升起摄像机"..
          "  down      ▶ 空格放开落下摄像机"..
          "  follow    ▶ 开启摄像机跟随"..
          "  no-follow ▶ 关闭摄像机跟随"..
          "  look      ▶ 开启摄像机看球"..
          "  no-look   ▶ 关闭摄像机看球"
  )
end
function CamManager:OnDestroy() 
  Game.Mediator:UnRegisterEventEmitter('CamManager')
  Game.Manager.GameDebugCommandServer:UnRegisterCommand(self._CommandId)
end

function CamManager:FixedUpdate()
  --摄像机水平旋转
  if self._CamIsRotateing then
    self._CamRotateTick = self._CamRotateTick + Time.deltaTime

    local v = self._CamRotateSpeedCurve:Evaluate(self._CamRotateTick / self._CameraRotateTime)
    
    self._CamRotateValueNow = self._CamRotateStartDegree + v * self._CamRotateTargetDegree
    self._CamOrientTransform.localEulerAngles = Vector3(0, self._CamRotateValueNow, 0)

    if v >= 1 then
      self._CamRotateValueNow = self._CamRotateStartDegree + self._CamRotateTargetDegree
      self._CamOrientTransform.localEulerAngles = Vector3(0, self._CamRotateValueNow, 0)
      self._CamIsRotateing = false
      self:ResetVector()
    end
  end
  --摄像机垂直向上
  if self._CamIsRotateingUp then
    self._CamRotateUpTick = self._CamRotateUpTick + Time.deltaTime
    
    local v = 0
    if self.CamIsSpaced then
      v = self._CamUpSpeedCurve:Evaluate(self._CamRotateUpTick / self._CameraRotateUpTime)
    else
      v = self._CamRotateSpeedCurve:Evaluate(self._CamRotateUpTick / self._CameraRotateUpTime)
    end
    self._CamPosFrame.localPosition = Vector3(0, self._CamRotateUpStart.y + v * self._CamRotateUpTarget.y, self._CamRotateUpStart.z + v * self._CamRotateUpTarget.z)
    if v >= 1 then
      self._CamIsRotateingUp = false
    end
  end
end

---摄像机面对向量重置
function CamManager:ResetVector()
  --根据摄像机朝向重置几个球推动的方向向量
  local y = -self._CamOrientTransform.localEulerAngles.y - 90
  self.CamRightVector = Quaternion.AngleAxis(-y, Vector3.up) * Vector3.right
  self.CamLeftVector = Quaternion.AngleAxis(-y, Vector3.up) * Vector3.left
  self.CamForwerdVector = Quaternion.AngleAxis(-y, Vector3.up) * Vector3.forward
  self.CamBackVector = Quaternion.AngleAxis(-y, Vector3.up) * Vector3.back
end
function CamManager:_UpdateStateForDebugStats()
  if BALLANCE_DEBUG then 
    GameUI.GamePlayUI._DebugStatValues['CamDirection'].Value = tostring(self._CamOrientTransform.localEulerAngles.y)
    GameUI.GamePlayUI._DebugStatValues['CamState'].Value = 'IsSpaced: '..tostring(self.CamIsSpaced)
      ..' Follow: '..tostring(self.CamFollow.Follow)..' Look: '..tostring(self.CamFollow.Look)
  end
end

--#region 公共方法

---通过RestPoint占位符设置摄像机的方向和位置
---@param go GameObject RestPoint占位符
function CamManager:SetPosAndDirByRestPoint(go) 
  local rot = go.transform.eulerAngles.y
  rot = rot % 360
  if rot < 0 then rot = rot + 360 
  elseif rot > 315 then rot = rot - 360 
  end

  self._CamOrientTransform.localEulerAngles = Vector3(0, rot - 90, 0)
  self._CamTarget.position = go.transform.position
  self.transform.position = self._CamPosFrame.position
  self.CamRotateValue = rot - 90
  self._CamRotateValueNow = rot - 90
  self:ResetVector()
  self:_UpdateStateForDebugStats()
  return self
end
---空格键向上旋转
---@param enable boolean 状态
function CamManager:RotateUp(enable)
  self.CamIsSpaced = enable
  self._CamRotateUpStart.y = self._CamPosFrame.localPosition.y
  self._CamRotateUpStart.z = self._CamPosFrame.localPosition.z
  if enable then
    self._CamRotateUpTarget.y = self._CameraSpaceY - self._CamRotateUpStart.y
    self._CamRotateUpTarget.z = -self._CameraSpaceZ - self._CamRotateUpStart.z
  else
    self._CamRotateUpTarget.y = self._CameraNormalY - self._CamRotateUpStart.y
    self._CamRotateUpTarget.z = -self._CameraNormalZ - self._CamRotateUpStart.z
  end
  self._CamRotateUpTick = 0
  self._CamIsRotateingUp = true
  self.EventRotateUpStateChanged:Emit(enable)
  self:_UpdateStateForDebugStats()
  return self
end
---摄像机向右旋转
function CamManager:RotateRight()
  self:RotateDregree(90)
  return self
end
---摄像机向左旋转
function CamManager:RotateLeft()
  self:RotateDregree(-90)
  return self
end
---摄像机旋转指定度数
---@param deg number 度数，正数往右，负数往左
---@return CamManager
function CamManager:RotateDregree(deg)
  self.CamRotateValue = self.CamRotateValue + deg
  self._CamRotateStartDegree = self._CamRotateValueNow
  self._CamRotateTargetDegree = self.CamRotateValue - self._CamRotateStartDegree
  self._CamRotateTick = 0
  self._CamIsRotateing = true
  self.EventRotateDirectionChanged:Emit(self._CamRotateTargetDegree)
  self:_UpdateStateForDebugStats()
  return self
end
---设置主摄像机天空盒材质
---@param mat Material
function CamManager:SetSkyBox(mat)
  self._SkyBox.material = mat
  return self
end
---指定摄像机是否开启跟随球
---@param enable boolean
function CamManager:SetCamFollow(enable)
  self.CamFollow.Follow = enable
  self.EventCamFollowChanged:Emit(enable)
  self:_UpdateStateForDebugStats()
  return self
end
---指定摄像机是否开启看着球
---@param enable boolean
function CamManager:SetCamLook(enable)
  self.CamFollow.Look = enable
  self.EventCamLookChanged:Emit(enable)
  self:_UpdateStateForDebugStats()
  return self
end
---指定当前摄像机跟踪的目标
---@param target Transform 目标
---@param noUpdatePos boolean|nil 禁止设置目标时的位置同步
function CamManager:SetTarget(target, noUpdatePos)
  if noUpdatePos then
    self.CamFollow:SetTargetWithoutUpdatePos(target)
  else
    self.CamFollow.Target = target
  end
  self.Target = target
  self.EventCamFollowTargetChanged:Emit(target)
  return self
end
---禁用所有摄像机功能
---@return CamManager
function CamManager:DisbleAll()
  self.CamFollow.Follow = false
  self.CamFollow.Look = false
  self.CamFollow.Target = nil
  self.EventCamFollowChanged:Emit(false)
  self.EventCamLookChanged:Emit(false)
  self.EventCamFollowTargetChanged:Emit(nil)
  self:_UpdateStateForDebugStats()
  return self
end

--#endregion

function CreateClass:CamManager()
  return CamManager()
end