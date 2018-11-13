﻿/****************************************************************************
Copyright (c) 2015 Lingjijian

Created by Lingjijian on 2015

342854406@qq.com

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
****************************************************************************/
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using System.Collections;

namespace QFramework
{
    /// <summary>
    /// 按钮
    /// </summary>
    #if SLUA_SUPPORT 
    [SLua.CustomLuaClass] 
    #endif
    public class LButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler,IPointerExitHandler
    {
        public const float LONGPRESS_TIME = 0.5f;
        public UnityAction onLongClickHandler;
        public UnityAction onLongClickUpdate;
        protected bool _isRunning;
        public UnityAction onPointerDown;
        public UnityAction onPointerUp;
        public UnityAction onPointerExit;

        public LButton()
        {
            _isRunning = false;
        }
#if SLUA_SUPPORT
		[SLua.DoNotToLua]
#endif
        public void OnPointerDown(PointerEventData eventData)
        {
            
            Invoke("executeLongClickHandler", LONGPRESS_TIME);
            if (onPointerDown != null)
            {
                onPointerDown.Invoke();
            }
        }

#if SLUA_SUPPORT
		[SLua.DoNotToLua]
#endif
        public void OnPointerUp(PointerEventData eventData)
        {
            _isRunning = false;
            CancelInvoke();
            if (onPointerUp != null)
            {
                onPointerUp.Invoke();
            }
        }

#if SLUA_SUPPORT
		[SLua.DoNotToLua]
#endif
        public void OnPointerExit(PointerEventData eventData)
        {
            _isRunning = false;
            CancelInvoke();
            if (onPointerExit != null)
            {
                onPointerExit.Invoke();
            }
        }

        protected void executeLongClickHandler()
        {
            _isRunning = true;
            if (onLongClickHandler != null)
            {
                onLongClickHandler.Invoke();
            }
        }
        
        void Update()
        {
            if (_isRunning && onLongClickUpdate != null)
            {
                onLongClickUpdate.Invoke();
            }
        }
    }
}
