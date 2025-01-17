local GameUIControlMessageSender = Ballance2.UI.Core.GameUIControlMessageSender

---鼠标外框选择小工具
---@class MouseSelect : GameLuaObjectHostClass
local MouseSelect = {
  QuickOutline = nil,---@type QuickOutline
  NormalColor = nil,---@type Color
  HoverColor = nil,---@type Color
  MessageName = nil,---@type string
  MessageSender = nil,---@type GameUIControlMessageSender
}

function CreateClass:MouseSelect()
  
  function MouseSelect:new(o)
    o = o or {}
    setmetatable(o, self)
    self.__index = self
    return o
  end

  function MouseSelect:Start()
    self.MessageSender = self.gameObject:GetComponent(GameUIControlMessageSender)
  end
  function MouseSelect:OnMouseEnter()
    self.QuickOutline.OutlineColor = self.HoverColor
  end
  function MouseSelect:OnMouseExit()
    self.QuickOutline.OutlineColor = self.NormalColor
  end
  function MouseSelect:OnMouseDown()
    self.MessageSender:NotifyEvent(self.MessageName)
  end

  return MouseSelect:new(nil)
end