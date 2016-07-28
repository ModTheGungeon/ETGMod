using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using InControl;

class MultiplayerActions : GungeonActions {

    public static MultiplayerActions baseActions;

    patch_TwoAxisInputControl cont = new patch_TwoAxisInputControl();

    public new TwoAxisInputControl Move {
        get {

            int X = 0;
            if (NetworkInput.directions[1]==1&&NetworkInput.directions[4]!=1) {
                if (NetworkInput.directions[1]==1)
                    X=1;
                if (NetworkInput.directions[1]==1)
                    X=-1;
            }

            int Y = 0;
            if (NetworkInput.directions[0]==1&&NetworkInput.directions[3]!=1) {
                if (NetworkInput.directions[3]==1)
                    Y=1;
                if (NetworkInput.directions[3]==1)
                    Y=-1;
            }

            cont.X=X;
            cont.Y=Y;
            return cont;
        }
    }

    public MultiplayerActions() : base() {
        baseActions=this;
    }

}

