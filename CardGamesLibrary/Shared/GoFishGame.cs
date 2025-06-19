using System;
using System.Collections.Generic;
using System.Linq;

namespace CardGamesLibrary.Shared
{
 

    public class Player
    {
        public string Name { get; set; } = "";
        public List<Card> Hand { get; set; } = new();
        public List<Rank> Books { get; set; } = new();
    }

    // This file has been removed. All Go Fish logic and state is now in GoFishService.cs.
}
