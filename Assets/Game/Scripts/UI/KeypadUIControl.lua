local EventTriggerListener = Ballance2.Services.InputManager.EventTriggerListener
local GameManager = Ballance2.Services.GameManager
local KeyPadJoystickController = Ballance2.Game.KeyPadJoystickController
local KeyPadJoystickDirection = Ballance2.Game.KeyPadJoystickDirection
local SimpleTouchDirectionKeyController = Ballance2.Game.SimpleTouchDirectionKeyController

---手机上的游戏控制键盘控制类
---@class KeypadUIControl : GameLuaObjectHostClass
KeypadUIControl = ClassicObject:extend()

function KeypadUIControl:new() 
  self.shiftPressCount = 0
  self.shiftPressOne = false
  self.lastCameraSpaceByShift = false
end
function KeypadUIControl:Start() 
  --自动扫描开头为Button的对象作为按钮，最多扫描2级
  for i = 0, self.transform.childCount - 1, 1 do
    local child = self.transform:GetChild(i);
    if(string.startWith(child.gameObject.name, "Button") or child.gameObject.name == "Joystick" or child.gameObject.name == "DirectionKey") then
      self:AddButton(child.gameObject);
    elseif child.childCount > 0 then
      for j = 0, child.childCount - 1, 1 do
        local child2 = child:GetChild(j).gameObject;
        if(string.startWith(child2.name, "Button")) then
          self:AddButton(child2);
        end
      end
    end
  end
end
---添加按钮
---@param go GameObject
function KeypadUIControl:AddButton(go) 
  local name = go.name;
  local CamManager = Game.GamePlay.CamManager
  local BallManager = Game.GamePlay.BallManager

  local enumBack = KeyPadJoystickDirection.Back;
  local enumForward = KeyPadJoystickDirection.Forward;
  local enumLeft = KeyPadJoystickDirection.Left;
  local enumRight = KeyPadJoystickDirection.Right;

  if name == "Joystick" then

    local Joystick = go:GetComponent(KeyPadJoystickController) ---@type KeyPadJoystickController
    Joystick.ValueChanged = function (x, y)
      BallManager:SetBallPushValue(x, y)
    end

  elseif name == "DirectionKey" then

    local DirectionKey = go:GetComponent(SimpleTouchDirectionKeyController) ---@type SimpleTouchDirectionKeyController
    DirectionKey.DirectionChanged = function (state, dir)
      if dir == enumBack then
        BallManager.KeyStateBack = state
        BallManager:FlushBallPush()
      elseif dir == enumForward then
        BallManager.KeyStateForward = state
        BallManager:FlushBallPush()
      elseif dir == enumLeft then
        BallManager.KeyStateLeft = state
        BallManager:FlushBallPush()
      elseif dir == enumRight then
        BallManager.KeyStateRight = state
        BallManager:FlushBallPush()
      end
    end
    
  else
    local listener = EventTriggerListener.Get(go);
    if name == "ButtonUp" then

      --只有调试模式才显示
      if not GameManager.DebugMode then
        go:SetActive(false)
      else
        --上升键
        listener.onDown = function () 
          BallManager.KeyStateDown = true
          BallManager:FlushBallPush()
        end
        listener.onUp = function () 
          BallManager.KeyStateDown = false
          BallManager:FlushBallPush()
        end
      end


    elseif name == "ButtonDown" then

      --只有调试模式才显示
      if not GameManager.DebugMode then
        go:SetActive(false)
      else
        --下降键
        listener.onDown = function () 
          BallManager.KeyStateUp = true
          BallManager:FlushBallPush()
        end
        listener.onUp = function () 
          BallManager.KeyStateUp = false
          BallManager:FlushBallPush()
        end
      end

    elseif name == "ButtonSpaceShift" then

      --shift和space二合一键
      listener.onDown = function () 
        self.shiftPressCount = self.shiftPressCount + 1
        if self.shiftPressCount >= 2 and GamePlay.BallManager.CanControllCamera then
          self.lastCameraSpaceByShift = true
          CamManager:RotateUp(true)
        end
      end
      listener.onUp = function () 
        self.shiftPressCount = self.shiftPressCount - 1
        if self.lastCameraSpaceByShift and GamePlay.BallManager.CanControllCamera then
          self.lastCameraSpaceByShift = false
          CamManager:RotateUp(false)
        end
      end
    elseif name == "ButtonSpace" then

      --空格键
      listener.onDown = function () 
        if GamePlay.BallManager.CanControllCamera then
          CamManager:RotateUp(true)  
        end
      end
      listener.onUp = function () 
        CamManager:RotateUp(false) 
      end

    elseif name == "ButtonShift" then

      --shift键
      listener.onDown = function () self.shiftPressOne = true end
      listener.onUp = function () self.shiftPressOne = false end

    elseif name == "ButtonCamLeft" then

      --摄像机左转键
      listener.onClick = function () 
        if GamePlay.BallManager.CanControllCamera then
          CamManager:RotateLeft() 
        end
      end

    elseif name == "ButtonCamRight" then

      --摄像机右转键
      listener.onClick = function ()
        if GamePlay.BallManager.CanControllCamera then
          CamManager:RotateRight() 
        end
      end

    end
  end
end

function CreateClass:KeypadUIControl() 
  return KeypadUIControl()
end