using FairyGUI;
using System.Collections;
using UnityEngine;

namespace FairyGame
{
	public class MyGLoader : GLoader
	{
		public MyGLoader()
		{
		}

		protected override void LoadExternal()
		{
			//IconManager.inst.GetIcon(url, OnLoadCompleted);"
		}

		protected override void FreeExternal(NTexture texture)
		{
			texture.refCount--;
		}

		private void OnLoadCompleted(string assetPath, string fileName, object data)
		{
			if (fileName != this.url) //Loader已经被另外设置了
				return;

			if (data != null)
			{
				NTexture texture = (NTexture)data;
				texture.refCount++;
				this.onExternalLoadSuccess(texture);
			}
			else
				this.onExternalLoadFailed();
		}
	}
}
