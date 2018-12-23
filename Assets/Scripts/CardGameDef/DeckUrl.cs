﻿/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using Newtonsoft.Json;

namespace CardGameDef
{
    [JsonObject(MemberSerialization.OptIn)]
    public class DeckUrl
    {
        [JsonProperty]
        public string Name { get; private set; }

        [JsonProperty]
        public string Url { get; private set; }

        [JsonConstructor]
        public DeckUrl(string name, string url)
        {
            Name = name ?? string.Empty;
            Url = url ?? string.Empty;
        }
    }
}
