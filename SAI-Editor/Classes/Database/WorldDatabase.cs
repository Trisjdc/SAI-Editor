﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using SAI_Editor.Classes.Database.Classes;
using SAI_Editor.Enumerators;
using SAI_Editor.Forms;
using System.Linq;

namespace SAI_Editor.Classes.Database
{
    class WorldDatabase : Database<MySqlConnection, MySqlConnectionStringBuilder, MySqlParameter, MySqlCommand, MySqlTransaction>
    {
        public WorldDatabase(string host, uint port, string username, string password, string databaseName)
        {
            connectionString = new MySqlConnectionStringBuilder();
            connectionString.Server = host;
            connectionString.Port = port;
            connectionString.UserID = username;
            connectionString.Password = password;
            connectionString.Database = databaseName;
            connectionString.AllowUserVariables = true;
            connectionString.AllowZeroDateTime = true;
        }

        public async Task<int> GetCreatureIdByGuid(int guid)
        {
            DataTable dt = await ExecuteQuery("SELECT id FROM creature WHERE guid = '" + guid + "'");

            if (dt.Rows.Count == 0)
                return 0;

            return CustomConverter.ToInt32(dt.Rows[0]["id"]);
        }

        public async Task<int> GetGameobjectIdByGuid(int guid)
        {
            DataTable dt = await ExecuteQuery("SELECT id FROM gameobject WHERE guid = '" + guid + "'");

            if (dt.Rows.Count == 0)
                return 0;

            return CustomConverter.ToInt32(dt.Rows[0]["id"]);
        }

        public async Task<int> GetObjectIdByGuidAndSourceType(int guid, int source_type)
        {
            switch ((SourceTypes)source_type)
            {
                case SourceTypes.SourceTypeCreature:
                    return await GetCreatureIdByGuid(guid);
                case SourceTypes.SourceTypeGameobject:
                    return await GetGameobjectIdByGuid(guid);
            }

            return 0;
        }

        public async Task<string> GetObjectAiName(int entryorguid, int source_type)
        {
            if (entryorguid < 0)
                entryorguid = await GetObjectIdByGuidAndSourceType(entryorguid, source_type);

            switch ((SourceTypes)source_type)
            {
                case SourceTypes.SourceTypeCreature:
                    return await GetCreatureAiNameById(entryorguid);
                case SourceTypes.SourceTypeGameobject:
                    return await GetGameobjectAiNameById(entryorguid);
                case SourceTypes.SourceTypeAreaTrigger:
                    return await GetAreaTriggerScriptNameById(entryorguid);
            }

            return String.Empty;
        }

        public async Task<string> GetCreatureAiNameById(int id)
        {
            DataTable dt = await ExecuteQuery("SELECT ainame FROM creature_template WHERE entry = '" + id + "'");

            if (dt.Rows.Count == 0)
                return String.Empty;

            return dt.Rows[0]["ainame"].ToString();
        }

        public async Task<string> GetCreatureScriptNameById(int id)
        {
            DataTable dt = await ExecuteQuery("SELECT scriptname FROM creature_template WHERE entry = '" + id + "'");

            if (dt.Rows.Count == 0)
                return String.Empty;

            return dt.Rows[0]["scriptname"].ToString();
        }

        public async Task<string> GetGameobjectAiNameById(int id)
        {
            DataTable dt = await ExecuteQuery("SELECT ainame FROM gameobject_template WHERE entry = '" + id + "'");

            if (dt.Rows.Count == 0)
                return String.Empty;

            return dt.Rows[0]["ainame"].ToString();
        }

        public async Task<string> GetGameobjectScriptNameById(int id)
        {
            DataTable dt = await ExecuteQuery("SELECT scriptname FROM gameobject_template WHERE entry = '" + id + "'");

            if (dt.Rows.Count == 0)
                return String.Empty;

            return dt.Rows[0]["scriptname"].ToString();
        }

        public async Task<string> GetAreaTriggerScriptNameById(int id)
        {
            DataTable dt = await ExecuteQuery("SELECT scriptname FROM areatrigger_scripts WHERE entry = '" + id + "'");

            if (dt.Rows.Count == 0)
                return String.Empty;

            return dt.Rows[0]["scriptname"].ToString();
        }

        public async Task<string> GetCreatureNameById(int id)
        {
            DataTable dt = await ExecuteQuery("SELECT name FROM creature_template WHERE entry = '" + id + "'");

            if (dt.Rows.Count == 0)
                return String.Empty;

            return dt.Rows[0]["name"].ToString();
        }

        public async Task<string> GetCreatureNameByGuid(int guid)
        {
            DataTable dt = await ExecuteQuery("SELECT `name` FROM creature_template WHERE entry = '" + await GetCreatureIdByGuid(guid) + "'");

            if (dt.Rows.Count == 0)
                return String.Empty;

            return dt.Rows[0]["name"].ToString();
        }

        public async Task<string> GetGameobjectNameById(int id)
        {
            DataTable dt = await ExecuteQuery("SELECT name FROM gameobject_template WHERE entry = '" + id + "'");

            if (dt.Rows.Count == 0)
                return String.Empty;

            return dt.Rows[0]["name"].ToString();
        }

        public async Task<string> GetGameobjectNameByGuid(int guid)
        {
            DataTable dt = await ExecuteQuery("SELECT name FROM gameobject_template WHERE entry = '" + await GetGameobjectIdByGuid(guid) + "'");

            if (dt.Rows.Count == 0)
                return String.Empty;

            return dt.Rows[0]["name"].ToString();
        }

        public async Task<string> GetObjectNameByIdAndSourceType(int id, int source_type)
        {
            switch ((SourceTypes)source_type)
            {
                case SourceTypes.SourceTypeCreature:
                    return await GetCreatureNameById(id);
                case SourceTypes.SourceTypeGameobject:
                    return await GetGameobjectNameById(id);
            }

            return String.Empty;
        }

        public async Task<string> GetObjectNameByIdOrGuidAndSourceType(SourceTypes sourceType, int idOrGuid, bool errorIfNoneFound = false)
        {
            string newName = String.Empty;

            switch (sourceType)
            {
                case SourceTypes.SourceTypeCreature:
                    newName = idOrGuid < 0 ? await GetCreatureNameByGuid(-idOrGuid) : await GetCreatureNameById(idOrGuid);

                    if (errorIfNoneFound && String.IsNullOrWhiteSpace(newName))
                        newName = "<Could not generate name>";

                    return newName;
                case SourceTypes.SourceTypeGameobject:
                    newName = idOrGuid < 0 ? await GetGameobjectNameByGuid(-idOrGuid) : await GetGameobjectNameById(idOrGuid);

                    if (errorIfNoneFound && String.IsNullOrWhiteSpace(newName))
                        newName = "<Could not generate name>";

                    return newName;
                case SourceTypes.SourceTypeAreaTrigger:
                    return "Areatrigger";
                case SourceTypes.SourceTypeScriptedActionlist:
                    return "Actionlist " + idOrGuid;
            }

            return "<Could not generate name>";
        }

        public async Task<string> GetObjectNameByIdOrGuidAndSourceType(EntryOrGuidAndSourceType entryOrGuidAndSourceType, bool errorIfNoneFound = false)
        {
            return await GetObjectNameByIdOrGuidAndSourceType(entryOrGuidAndSourceType.sourceType, entryOrGuidAndSourceType.entryOrGuid, errorIfNoneFound);
        }

        public async Task<List<SmartScript>> GetSmartScripts(int entryorguid, int source_type)
        {
            DataTable dt = await ExecuteQuery("SELECT * FROM smart_scripts WHERE entryorguid = '" + entryorguid + "' AND source_type = '" + source_type + "'");

            if (dt.Rows.Count == 0)
                return null;

            List<SmartScript> d = (from DataRow row in dt.Rows select BuildSmartScript(row)).ToList();

            return (from DataRow row in dt.Rows select BuildSmartScript(row)).ToList();
        }

        public async Task<List<SmartScript>> GetSmartScriptsWithoutSourceType(int entryorguid, int source_type)
        {
            DataTable dt = await ExecuteQuery("SELECT * FROM smart_scripts WHERE entryorguid = '" + entryorguid + "' AND source_type != '" + source_type + "'");

            if (dt.Rows.Count == 0)
                return null;

            return (from DataRow row in dt.Rows select BuildSmartScript(row)).ToList();
        }

        public async Task<List<SmartScript>> GetSmartScriptActionLists(string criteria, bool useLikeStatement)
        {
            DataTable dt = await ExecuteQuery("SELECT * FROM smart_scripts WHERE action_type IN (80,87,88) AND source_type != 9 ORDER BY entryorguid");

            if (dt.Rows.Count == 0)
                return null;

            List<SmartScript> smartScriptsClean = new List<SmartScript>();

            List<SmartScript> smartScripts = (from DataRow row in dt.Rows select BuildSmartScript(row)).ToList();

            if (criteria.Length > 0)
            {
                foreach (SmartScript smartScript in smartScripts)
                {
                    var timedActionlistEntries = new List<string>();

                    switch ((SmartAction)smartScript.action_type)
                    {
                        case SmartAction.SMART_ACTION_CALL_TIMED_ACTIONLIST:
                            timedActionlistEntries.Add(smartScript.action_param1.ToString());
                            break;
                        case SmartAction.SMART_ACTION_CALL_RANDOM_TIMED_ACTIONLIST:
                            timedActionlistEntries.Add(smartScript.action_param1.ToString());
                            timedActionlistEntries.Add(smartScript.action_param2.ToString());

                            if (smartScript.action_param3 > 0)
                                timedActionlistEntries.Add(smartScript.action_param3.ToString());

                            if (smartScript.action_param4 > 0)
                                timedActionlistEntries.Add(smartScript.action_param4.ToString());

                            if (smartScript.action_param5 > 0)
                                timedActionlistEntries.Add(smartScript.action_param5.ToString());

                            if (smartScript.action_param6 > 0)
                                timedActionlistEntries.Add(smartScript.action_param6.ToString());

                            break;
                        case SmartAction.SMART_ACTION_CALL_RANDOM_RANGE_TIMED_ACTIONLIST:
                            for (int i = smartScript.action_param1; i <= smartScript.action_param2; ++i)
                                timedActionlistEntries.Add(i.ToString());
                            break;
                    }

                    foreach (string scriptEntry in timedActionlistEntries)
                    {
                        if (useLikeStatement)
                        {
                            if (scriptEntry.IndexOf(criteria, StringComparison.OrdinalIgnoreCase) >= 0)
                                smartScriptsClean.Add(smartScript);
                        }
                        else if (scriptEntry.IndexOf(criteria) >= 0)
                            smartScriptsClean.Add(smartScript);
                    }
                }
            }
            else
                smartScriptsClean = smartScripts;

            return smartScriptsClean;
        }

        public async Task<List<SmartScript>> GetSmartScriptsCallingActionLists()
        {
            DataTable dt = await ExecuteQuery("SELECT * FROM smart_scripts WHERE action_type IN (80,87,88) AND source_type != 9 ORDER BY entryorguid");

            if (dt.Rows.Count == 0)
                return null;

            return (from DataRow row in dt.Rows select BuildSmartScript(row)).ToList();
        }

        public async Task<bool> AreaTriggerHasSmartAI(int entry)
        {
            DataTable dt = await ExecuteQuery("SELECT * FROM areatrigger_scripts WHERE ScriptName = 'SmartTrigger' AND entry = '" + entry + "'");
            return dt.Rows.Count > 0;
        }

        public async Task<string> GetQuestTitleById(int id)
        {
            DataTable dt = await ExecuteQuery("SELECT title FROM quest_template WHERE id = '" + id + "'");

            if (dt.Rows.Count == 0)
                return String.Empty;

            return dt.Rows[0]["title"].ToString();
        }

        public async Task<string> GetQuestTitleByCriteria(int requiredNpcOrGo1, int requiredNpcOrGo2, int requiredNpcOrGo3, int requiredNpcOrGo4)
        {
            DataTable dt = await ExecuteQuery("SELECT title FROM quest_template WHERE (RequiredNpcOrGo1 = '" + requiredNpcOrGo1 + "' OR " + "RequiredNpcOrGo1 = '" + requiredNpcOrGo2 + "' OR " + "RequiredNpcOrGo2 = '" + requiredNpcOrGo3 + "' OR " + "RequiredNpcOrGo3 = '" + requiredNpcOrGo3 + "' OR " + "RequiredNpcOrGo4 = '" + requiredNpcOrGo4 + "')");

            if (dt.Rows.Count == 0)
                return String.Empty;

            return dt.Rows[0]["title"].ToString();
        }

        public async Task<string> GetQuestTitleByCriteria(int requiredNpcOrGo1, int requiredNpcOrGo2, int requiredNpcOrGo3, int requiredNpcOrGo4, int requiredSpellCast1)
        {
            DataTable dt = await ExecuteQuery("SELECT title FROM quest_template WHERE (RequiredNpcOrGo1 = '" + requiredNpcOrGo1 + "' OR " + "RequiredNpcOrGo1 = '" + requiredNpcOrGo2 + "' OR " + "RequiredNpcOrGo2 = '" + requiredNpcOrGo3 + "' OR " + "RequiredNpcOrGo3 = '" + requiredNpcOrGo3 + "' OR " + "RequiredNpcOrGo4 = '" + requiredNpcOrGo4 + "') AND RequiredSpellCast1 = '" + requiredSpellCast1 + "'");

            if (dt.Rows.Count == 0)
                return String.Empty;

            return dt.Rows[0]["title"].ToString();
        }

        public async Task<string> GetItemNameById(int entry)
        {
            DataTable dt = await ExecuteQuery("SELECT name FROM item_template WHERE entry = '" + entry + "'");

            if (dt.Rows.Count == 0)
                return String.Empty;

            return dt.Rows[0]["name"].ToString();
        }

        public async Task<List<Creature>> GetCreaturesById(int id)
        {
            DataTable dt = await ExecuteQuery("SELECT * FROM creature WHERE id = '" + id + "'");

            if (dt.Rows.Count == 0)
                return null;

            return (from DataRow row in dt.Rows select BuildCreature(row)).ToList();
        }

        public async Task<List<Gameobject>> GetGameobjectsById(int id)
        {
            DataTable dt = await ExecuteQuery("SELECT * FROM gameobject WHERE id = '" + id + "'");

            if (dt.Rows.Count == 0)
                return null;

            return (from DataRow row in dt.Rows select BuildGameobject(row)).ToList();
        }

        private SmartScript BuildSmartScript(DataRow row)
        {
            var smartScript = new SmartScript();
            smartScript.entryorguid = row["entryorguid"] != DBNull.Value ? CustomConverter.ToInt32(row["entryorguid"]) : -1;
            smartScript.source_type = row["source_type"] != DBNull.Value ? CustomConverter.ToInt32(row["source_type"]) : 0;
            smartScript.id = row["id"] != DBNull.Value ? CustomConverter.ToInt32(row["id"]) : 0;
            smartScript.link = row["link"] != DBNull.Value ? CustomConverter.ToInt32(row["link"]) : 0;
            smartScript.event_type = row["event_type"] != DBNull.Value ? CustomConverter.ToInt32(row["event_type"]) : 0;
            smartScript.event_phase_mask = row["event_phase_mask"] != DBNull.Value ? CustomConverter.ToInt32(row["event_phase_mask"]) : 0;
            smartScript.event_chance = row["event_chance"] != DBNull.Value ? CustomConverter.ToInt32(row["event_chance"]) : 0;
            smartScript.event_flags = row["event_flags"] != DBNull.Value ? CustomConverter.ToInt32(row["event_flags"]) : 0;
            smartScript.event_param1 = row["event_param1"] != DBNull.Value ? CustomConverter.ToInt32(row["event_param1"]) : 0;
            smartScript.event_param2 = row["event_param2"] != DBNull.Value ? CustomConverter.ToInt32(row["event_param2"]) : 0;
            smartScript.event_param3 = row["event_param3"] != DBNull.Value ? CustomConverter.ToInt32(row["event_param3"]) : 0;
            smartScript.event_param4 = row["event_param4"] != DBNull.Value ? CustomConverter.ToInt32(row["event_param4"]) : 0;
            smartScript.action_type = row["action_type"] != DBNull.Value ? CustomConverter.ToInt32(row["action_type"]) : 0;
            smartScript.action_param1 = row["action_param1"] != DBNull.Value ? CustomConverter.ToInt32(row["action_param1"]) : 0;
            smartScript.action_param2 = row["action_param2"] != DBNull.Value ? CustomConverter.ToInt32(row["action_param2"]) : 0;
            smartScript.action_param3 = row["action_param3"] != DBNull.Value ? CustomConverter.ToInt32(row["action_param3"]) : 0;
            smartScript.action_param4 = row["action_param4"] != DBNull.Value ? CustomConverter.ToInt32(row["action_param4"]) : 0;
            smartScript.action_param5 = row["action_param5"] != DBNull.Value ? CustomConverter.ToInt32(row["action_param5"]) : 0;
            smartScript.action_param6 = row["action_param6"] != DBNull.Value ? CustomConverter.ToInt32(row["action_param6"]) : 0;
            smartScript.target_type = row["target_type"] != DBNull.Value ? CustomConverter.ToInt32(row["target_type"]) : 0;
            smartScript.target_param1 = row["target_param1"] != DBNull.Value ? CustomConverter.ToInt32(row["target_param1"]) : 0;
            smartScript.target_param2 = row["target_param2"] != DBNull.Value ? CustomConverter.ToInt32(row["target_param2"]) : 0;
            smartScript.target_param3 = row["target_param3"] != DBNull.Value ? CustomConverter.ToInt32(row["target_param3"]) : 0;
            smartScript.target_x = row["target_x"] != DBNull.Value ? row["target_x"].ToString() : String.Empty;
            smartScript.target_y = row["target_y"] != DBNull.Value ? row["target_y"].ToString() : String.Empty;
            smartScript.target_z = row["target_z"] != DBNull.Value ? row["target_z"].ToString() : String.Empty;
            smartScript.target_o = row["target_o"] != DBNull.Value ? row["target_o"].ToString() : String.Empty;
            smartScript.comment = row["comment"] != DBNull.Value ? (string)row["comment"] : String.Empty;
            return smartScript;
        }

        private Creature BuildCreature(DataRow row)
        {
            var creature = new Creature();
            creature.guid = row["guid"] != DBNull.Value ? CustomConverter.ToInt32(row["guid"]) : 0;
            creature.id = row["id"] != DBNull.Value ? CustomConverter.ToInt32(row["id"]) : 0;
            creature.map = row["map"] != DBNull.Value ? CustomConverter.ToInt32(row["map"]) : 0;
            creature.spawnMask = row["spawnMask"] != DBNull.Value ? CustomConverter.ToInt32(row["spawnMask"]) : 0;
            creature.phaseMask = row["phaseMask"] != DBNull.Value ? CustomConverter.ToInt32(row["phaseMask"]) : 0;
            creature.modelid = row["modelid"] != DBNull.Value ? CustomConverter.ToInt32(row["modelid"]) : 0;
            creature.equipment_id = row["equipment_id"] != DBNull.Value ? CustomConverter.ToInt32(row["equipment_id"]) : 0;
            creature.position_x = row["position_x"] != DBNull.Value ? CustomConverter.ToInt32(row["position_x"]) : 0;
            creature.position_y = row["position_y"] != DBNull.Value ? CustomConverter.ToInt32(row["position_y"]) : 0;
            creature.position_z = row["position_z"] != DBNull.Value ? CustomConverter.ToInt32(row["position_z"]) : 0;
            creature.orientation = row["orientation"] != DBNull.Value ? CustomConverter.ToInt32(row["orientation"]) : 0;
            creature.spawntimesecs = row["spawntimesecs"] != DBNull.Value ? CustomConverter.ToInt32(row["spawntimesecs"]) : 0;
            creature.spawndist = row["spawndist"] != DBNull.Value ? CustomConverter.ToInt32(row["spawndist"]) : 0;
            creature.currentwaypoint = row["currentwaypoint"] != DBNull.Value ? CustomConverter.ToInt32(row["currentwaypoint"]) : 0;
            creature.curhealth = row["curhealth"] != DBNull.Value ? CustomConverter.ToInt32(row["curhealth"]) : 0;
            creature.curmana = row["curmana"] != DBNull.Value ? CustomConverter.ToInt32(row["curmana"]) : 0;
            creature.movementType = row["movementType"] != DBNull.Value ? CustomConverter.ToInt32(row["movementType"]) : 0;
            creature.npcflag = row["npcflag"] != DBNull.Value ? CustomConverter.ToInt32(row["npcflag"]) : 0;
            creature.unit_flags = row["unit_flags"] != DBNull.Value ? CustomConverter.ToInt32(row["unit_flags"]) : 0;
            creature.dynamicflags = row["dynamicflags"] != DBNull.Value ? CustomConverter.ToInt32(row["dynamicflags"]) : 0;
            return creature;
        }

        private Gameobject BuildGameobject(DataRow row)
        {
            var gameobject = new Gameobject();
            gameobject.guid = row["guid"] != DBNull.Value ? CustomConverter.ToInt32(row["guid"]) : 0;
            gameobject.id = row["id"] != DBNull.Value ? CustomConverter.ToInt32(row["id"]) : 0;
            gameobject.map = row["map"] != DBNull.Value ? CustomConverter.ToInt32(row["map"]) : 0;
            gameobject.spawnMask = row["spawnMask"] != DBNull.Value ? CustomConverter.ToInt32(row["spawnMask"]) : 0;
            gameobject.phaseMask = row["phaseMask"] != DBNull.Value ? CustomConverter.ToInt32(row["phaseMask"]) : 0;
            gameobject.position_x = row["position_x"] != DBNull.Value ? CustomConverter.ToInt32(row["position_x"]) : 0;
            gameobject.position_y = row["position_y"] != DBNull.Value ? CustomConverter.ToInt32(row["position_y"]) : 0;
            gameobject.position_z = row["position_z"] != DBNull.Value ? CustomConverter.ToInt32(row["position_z"]) : 0;
            gameobject.orientation = row["orientation"] != DBNull.Value ? CustomConverter.ToInt32(row["orientation"]) : 0;
            gameobject.rotation0 = row["rotation0"] != DBNull.Value ? CustomConverter.ToInt32(row["rotation0"]) : 0;
            gameobject.rotation1 = row["rotation1"] != DBNull.Value ? CustomConverter.ToInt32(row["rotation1"]) : 0;
            gameobject.rotation2 = row["rotation2"] != DBNull.Value ? CustomConverter.ToInt32(row["rotation2"]) : 0;
            gameobject.rotation3 = row["rotation3"] != DBNull.Value ? CustomConverter.ToInt32(row["rotation3"]) : 0;
            gameobject.spawntimesecs = row["spawntimesecs"] != DBNull.Value ? CustomConverter.ToInt32(row["spawntimesecs"]) : 0;
            gameobject.animprogress = row["animprogress"] != DBNull.Value ? CustomConverter.ToInt32(row["animprogress"]) : 0;
            gameobject.state = row["state"] != DBNull.Value ? CustomConverter.ToInt32(row["state"]) : 0;
            return gameobject;
        }
    }
}
