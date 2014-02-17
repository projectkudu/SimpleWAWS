using System;

namespace SimpleWAWS
{
    class SiteNameGenerator
    {
        private string[] _adjectives = new string[] { "Blue", "Green", "Orange", "Pink", "Purple", "Red", "Yellow", "Adventurous", "Bold", "Brave", "Bright", "Charming", "Cool", "Courageous",
            "Creative", "Dazzling", "Dancing", "Eager", "Fearless", "Friendly", "Heroic", "Important", "Impressive", "Ingenious", "Insightful", "Intuitive", "Masterful",
            "Mighty", "Modern", "Outstanding", "Smooth", "Patient", "Peaceful", "Popular", "Precise", "Productive", "Proficient", "Proud", "Resilient", "Resourceful", "Savvy", "Sociable",
            "Sharp", "Shiny", "Skillful", "Spirited", "Strong", "Stunning", "Successful", "Talented", "Technical", "Tenacious", "Upbeat", "Valuable", "Victorious",
            "Vigorous", "Visionary", "Vital", "Vivacious", "Winning", "Wise", "Youthful", "Zealous", "Zestful"
        };
        private string[] _nouns = new string[] { "Albatross", "Alligator", "Ant", "Armadillo", "Badger", "Bear", "Bee", "Bird", "Bison", "Buffalo", "Caribou", "Cheetah", "Cobra",
            "Coyote", "Crow", "Dinosaur", "Dragon", "Dolphin", "Dove", "Eagle", "Elephant", "Elk", "Falcon", "Fish", "Fox", "Frog", "Gazelle", "Panda", "Giraffe", "Gorilla",
            "Hamster", "Hawk", "Heron", "Hippo", "Horse", "Kangaroo", "Koala", "Kudu", "Lemur", "Leopard", "Lion", "Llama", "Lobster", "Manatee", "Meerkat", "Mink", "Mouse",
            "Narwhal", "Octopus", "Okapi", "Oryx", "Ostrich", "Otter", "Oyster", "Panther", "Parrot", "Pelican", "Penguin", "Pony", "Quail", "Rabbit", "Raven", "Rhino", "Robin",
            "Salmon", "Seahorse", "Seal", "Sparrow", "Spider", "Squid", "Squirrel", "Starling", "Stork", "Swan", "Tiger", "Trout", "Turkey", "Turtle", "Unicorn", "Walrus", "Wolf",
            "Cloud", "Galaxy", "Moon", "Mountain", "Prairie", "Star", "Valley", "Wind", "Rocket" };
        private Random _random = new Random();

        public string GenerateName(bool includeNumber)
        {
            // Return names like BlueTiger52349
            string name =_adjectives[_random.Next(_adjectives.Length)] +
                _nouns[_random.Next(_nouns.Length)];

            if (includeNumber)
            {
                name += _random.Next(1000);
            }

            return name;
        }
    }
}
