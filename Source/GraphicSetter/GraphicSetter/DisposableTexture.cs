using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GraphicSetter
{
    public class DisposableTexture : IDisposable
    {
        private readonly Texture2D texture;
        private bool disposed;

        public DisposableTexture(Texture2D texture)
        {
            this.texture = texture;
        }

        private void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (texture != null)
                {
                    Object.Destroy(texture);
                    disposed = true;
                }
            }
        }

        ~DisposableTexture()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
