using UnityEngine;
#if SLUA_SUPPORT 
    using SLua; 
#endif
using System.Collections;
#if SLUA_SUPPORT
[CustomLuaClass]
#endif
public class LWindowBase
{
//    public WindowDispose disposeType;
//    public WindowHierarchy hierarchy;

//    public LWindowBase()
//    {
//        this.disposeType = WindowDispose.Delay;
//    }
//#if SLUA_SUPPORT
//    [DoNotToLua]
//#endif
//    public virtual void Open(object[] list)
//    {
//        if (m_bReady)
//        {
//            m_cBehavior.OnWindowOpen(list);
//        }
//    }

//#if SLUA_SUPPORT
//    [DoNotToLua]
//#endif
//    public virtual void Close()
//    {
//        if (m_bReady)
//        {
//            m_cBehavior.OnWindowClose();
//        }
//    }
}

