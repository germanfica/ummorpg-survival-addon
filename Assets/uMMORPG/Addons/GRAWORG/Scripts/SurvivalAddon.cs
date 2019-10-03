using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Networking;
using System.IO;
using Mono.Data.Sqlite; // copied from Unity/Mono/lib/mono/2.0 to Plugins

public partial class Database
{
    static void Initialize_SurvivalAddon()
    {
        // create tables if they don't exist yet or were deleted
        // [PRIMARY KEY is important for performance: O(log n) instead of O(n)]
        ExecuteNonQuery(@"CREATE TABLE IF NOT EXISTS survival (
                            name TEXT NOT NULL PRIMARY KEY,
                            hunger INTEGER NOT NULL
                            )"); // Don't forget this )");
    }

    static void CharacterLoad_SurvivalAddon(Player player)
    {
        List<List<object>> table = ExecuteReader("SELECT * FROM survival WHERE name=@name", new SqliteParameter("@name", player.name));
        if (table.Count == 1)
        {
            List<object> mainrow = table[0];

            int hunger = Convert.ToInt32((long)mainrow[1]);
            player.hunger = hunger;
        }
    }
    
    static void CharacterSave_SurvivalAddon(Player player) {
        ExecuteNonQuery("INSERT OR REPLACE INTO survival VALUES (@name, @hunger)",
                        new SqliteParameter("@name", player.name),
                        new SqliteParameter("@hunger", player.hunger)
                        );
    }
}


public partial class Entity
{

    [Header("Survival")]
    [SerializeField] protected LevelBasedInt _hungerMax = new LevelBasedInt { baseValue = 100 };
    public virtual int hungerMax
    {
        get
        {
            // base + passives + buffs
            int passiveBonus = (from skill in skills
                                where skill.level > 0 && skill.data is PassiveSkill
                                select ((PassiveSkill)skill.data).bonusHealthMax.Get(skill.level)).Sum();
            int buffBonus = buffs.Sum(buff => buff.bonusHealthMax);
            return _healthMax.Get(level) + passiveBonus + buffBonus;
        }
    }

    [SyncVar] public int _hunger = 1;
    public int hunger
    {
        get { return Mathf.Min(_hunger, healthMax); } // min in case hp>hpmax after buff ends etc.
        set { _hunger = Mathf.Clamp(value, 0, healthMax); }
    }
}

