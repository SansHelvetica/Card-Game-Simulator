/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using Cgs.CardGameView;
using UnityEngine;
using UnityEngine.UI;

namespace Cgs.Play.Drawer
{
    public class TabTemplate : MonoBehaviour
    {
        public Button removeButton;
        public Toggle toggle;
        public Text nameText;
        public Text countText;
        public DrawerHandle drawerHandle;

        public int TabIndex { get; set; }
        public CardDropArea TabCardDropArea { get; set; }
        public CardDropArea CardZoneCardDropArea { get; set; }
    }
}
