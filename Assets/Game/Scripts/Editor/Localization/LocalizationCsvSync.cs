using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Localization;
using UnityEditor.Localization.Plugins.CSV;
using UnityEngine;

namespace NakeDev.Editor.Localization
{
    /// <summary>
    /// Ponte manual entre as String Table Collections da Unity Localization e os CSVs versionados
    /// em Assets/Localization/Tables — o mesmo arquivo que o Crowdin lê/escreve como fonte
    /// multilíngue (ver .Docs/crowdin.yml). Export roda sempre que texto novo é adicionado no
    /// Editor; Import roda depois que o Crowdin CLI baixa as traduções via CI.
    /// </summary>
    public static class LocalizationCsvSync
    {
        private const string CsvFolder = "Assets/Localization/Tables";

        [MenuItem("NakeDev/Localization/Export All Collections to CSV")]
        public static void ExportAll()
        {
            foreach (var collection in LocalizationEditorSettings.GetStringTableCollections())
            {
                string path = $"{CsvFolder}/{collection.TableCollectionName}.csv";
                using (var stream = new StreamWriter(path, false))
                {
                    Csv.Export(stream, collection);
                }
                AssetDatabase.ImportAsset(path);
                Debug.Log($"[LocalizationCsvSync] Exportado: {path}");
            }
        }

        [MenuItem("NakeDev/Localization/Import All Collections from CSV")]
        public static void ImportAll()
        {
            foreach (var collection in LocalizationEditorSettings.GetStringTableCollections())
            {
                string path = $"{CsvFolder}/{collection.TableCollectionName}.csv";
                if (!File.Exists(path))
                {
                    Debug.LogWarning($"[LocalizationCsvSync] CSV não encontrado, pulando: {path}");
                    continue;
                }

                using (var stream = new StreamReader(path))
                {
                    Csv.ImportInto(stream, collection);
                }
                Debug.Log($"[LocalizationCsvSync] Importado: {path}");
            }

            AssetDatabase.SaveAssets();
        }
    }
}
