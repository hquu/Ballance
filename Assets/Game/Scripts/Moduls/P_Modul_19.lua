local ObjectStateBackupUtils = Ballance2.Utils.ObjectStateBackupUtils

---P_Modul_19
---双向推板机关
---@class P_Modul_19 : ModulBase
---@field P_Modul_19_Flaps PhysicsObject
P_Modul_19 = ModulBase:extend()

function P_Modul_19:new()
  P_Modul_19.super.new(self)
  self.EnableBallRangeChecker = true
  self.BallCheckeRange = 60
  self.WakeUpLock = false
end

function P_Modul_19:Active()
  ModulBase.Active(self)
  self.WakeUpLock = false
  self.P_Modul_19_Flaps.gameObject:SetActive(true)
  self.P_Modul_19_Flaps:Physicalize()
  self.P_Modul_19_Flaps.CollisionID = GamePlay.BallSoundManager:GetSoundCollIDByName('Wood')
end
function P_Modul_19:Deactive()
  self.P_Modul_19_Flaps:UnPhysicalize(true)
  ModulBase.Deactive(self)
end

function P_Modul_19:ActiveForPreview()
  self.gameObject:SetActive(true)
end
function P_Modul_19:DeactiveForPreview()
  self.gameObject:SetActive(false)
end

function P_Modul_19:Reset()
  ObjectStateBackupUtils.RestoreObjectAndChilds(self.gameObject)
end
function P_Modul_19:Backup()
  ObjectStateBackupUtils.BackUpObjectAndChilds(self.gameObject)
end
function P_Modul_19:BallEnterRange()
  if not self.IsPreviewMode and not self.WakeUpLock and self.IsActive then
    self.WakeUpLock = true
    self.P_Modul_19_Flaps:WakeUp()
  end
end

function CreateClass:P_Modul_19()
  return P_Modul_19()
end