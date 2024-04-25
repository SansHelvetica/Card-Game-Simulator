/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

namespace Cgs.CardGameView.Multiplayer
{
    public class Token : CgsNetPlayable
    {
        public override string ViewValue => "Token";
        public override string DeletePrompt => "Delete token?";
    }
}
