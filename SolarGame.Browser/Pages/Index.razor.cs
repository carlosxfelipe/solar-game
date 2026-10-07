using System;
using Microsoft.JSInterop;
using Microsoft.Xna.Framework;

namespace SolarGame_Browser.Pages
{
    public partial class Index
    {
        SolarGame.Game1 _game;
        bool _wasPaused = true;

        protected override void OnAfterRender(bool firstRender)
        {
            base.OnAfterRender(firstRender);

            if (firstRender)
            {
                JsRuntime.InvokeAsync<object>("initRenderJS", DotNetObjectReference.Create(this));
            }
        }

        [JSInvokable]
        public bool ShouldLockPointer(int x, int y)
        {
            if (_game == null) return false;
            return _game.ShouldLockPointer(x, y);
        }

        [JSInvokable]
        public void TickDotNet(int dx, int dy)
        {
            // init game
            if (_game == null)
            {
                _game = new SolarGame.Game1();
                var jsInProcess = (IJSInProcessRuntime)JsRuntime;
                _game.OnPlayStepSound = () => jsInProcess.InvokeVoid("playStepSound");
                _game.Run();
            }

            _game.ExternalMouseDeltaX = dx;
            _game.ExternalMouseDeltaY = dy;

            // run gameloop
            _game.Tick();

            if (_game.IsPaused != _wasPaused)
            {
                _wasPaused = _game.IsPaused;
                if (_wasPaused)
                {
                    var jsInProcess = (IJSInProcessRuntime)JsRuntime;
                    jsInProcess.InvokeVoid("unlockPointer");
                }
            }
        }

    }
}
