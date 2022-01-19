using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/*
* Copyright(c) 2021  mengyu
*
* 模块名：     
* Progress.cs
* 
* 用途：
* 一个键盘按键选择组件。
*
* 作者：
* mengyu
*/

namespace Ballance2.UI.Core.Controls
{
  /// <summary>
  /// 一个键盘按键选择组件
  /// </summary>
  [SLua.CustomLuaClass]
  [AddComponentMenu("Ballance/UI/Controls/KeyChoose")]
  public class KeyChoose : MonoBehaviour
  {
    public Text Text;
    public Text TextValue;

    [SerializeField, SetProperty("value")]
    private KeyCode _value = KeyCode.None;

    public class KeyChooseEvent : UnityEvent<KeyCode>
    {
      public KeyChooseEvent() { }
    }

    public KeyChooseEvent onValueChanged = new KeyChooseEvent();

    /// <summary>
    /// 获取或设置按钮选中的键
    /// </summary>
    public KeyCode value
    {
      get { return _value; }
      set
      {
        _value = value;
        UpdateValue();
        if (onValueChanged != null)
          onValueChanged.Invoke(value);
      }
    }
    public void UpdateValue()
    {
      TextValue.text = _value.ToString();
    }

    void Start()
    {
      UpdateValue();
    }
    void Update()
    {
      if (Input.anyKeyDown && EventSystem.current.currentSelectedGameObject == gameObject)
      {
        value = Event.current.keyCode;
      }
    }
  }
}