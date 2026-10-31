#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Kael.EditorTools
{
    /// <summary>
    /// Fondo del Nivel 1 según el GDD:
    ///  - Primera zona: entrada a la zona industrial abandonada (exterior), con cielo contaminado,
    ///    fábricas lejanas con chimeneas humeando y fábricas cercanas con ventanas rotas, óxido y tuberías.
    ///    Usa parallax (las capas lejanas se mueven más lento) para dar profundidad.
    ///  - Sala de Oxibot Gigante (interior): paredes de metal, engranajes, tuberías y el puesto de control
    ///    con la energía cian del experimento de Kael. Al fondo, la salida hacia el exterior.
    ///
    /// Menú: Kael → Actualizar fondo del nivel (solo cambia el fondo de Nivel1; no toca nada más).
    /// </summary>
    public static class KaelBackgroundBuilder
    {
        const string Folder = "Assets/Sprites/Backgrounds/";
        const string LevelScene = "Assets/Scenes/Nivel1.unity";
        const int PPU = 16;

        /// <summary>Coordenada x del mundo donde empieza la sala del jefe (el arte SalaOxibot.png está hecho para x 109–152).</summary>
        const float BossRoomStartX = 109f;

        [MenuItem("Kael/Actualizar fondo del nivel", priority = 1)]
        public static void UpdateLevelBackground()
        {
            if (!File.Exists(LevelScene))
            {
                EditorUtility.DisplayDialog("Kael", "No existe " + LevelScene + ". Ejecute primero Kael → Construir proyecto.", "OK");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            Scene scene = EditorSceneManager.OpenScene(LevelScene);
            Camera cam = Camera.main != null ? Camera.main : Object.FindObjectOfType<Camera>();
            CameraFollow follow = cam != null ? cam.GetComponent<CameraFollow>() : null;
            float w = follow != null ? follow.maxBounds.x : 152f;
            float h = follow != null ? follow.maxBounds.y : 18f;

            // Quitar el fondo anterior
            DestroyIfExists(GameObject.Find("Fondo_Fabrica"));
            DestroyIfExists(GameObject.Find("Fondos"));
            if (cam != null) DestroyIfExists(cam.transform.Find("Cielo")?.gameObject);

            CreateBackgrounds(w, h, cam);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Kael] Fondo del nivel actualizado.");
            EditorUtility.DisplayDialog("Kael", "Fondo actualizado en la escena Nivel1.", "OK");
        }

        /// <summary>Crea todas las capas de fondo en la escena abierta. También lo usa el constructor del proyecto.</summary>
        public static void CreateBackgrounds(float levelWidth, float levelHeight, Camera cam)
        {
            Sprite sky = ImportSprite("Cielo.png");
            Sprite far = ImportSprite("FabricasLejanas.png");
            Sprite near = ImportSprite("FabricasCercanas.png");
            Sprite room = ImportSprite("SalaOxibot.png");

            var root = new GameObject("Fondos").transform;

            // 1) Cielo contaminado: hijo de la cámara (siempre detrás de todo)
            if (sky != null && cam != null)
            {
                var go = new GameObject("Cielo");
                go.transform.SetParent(cam.transform, false);
                go.transform.localPosition = new Vector3(0f, 0f, 20f);
                go.transform.localScale = new Vector3(240f, 4.5f, 1f); // 0.25x4 u → 60x18 u
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = sky;
                sr.sortingOrder = -40;
            }

            // 2) Fábricas lejanas con chimeneas (parallax lento)
            if (far != null)
                TiledLayer("FabricasLejanas", far, root, levelWidth, 8f, 1f, 0.8f, 0.8f, -30);

            // 3) Fábricas cercanas, tanque, tuberías y cerca (parallax medio)
            if (near != null)
                TiledLayer("FabricasCercanas", near, root, levelWidth, 10f, 0f, 0.5f, 0.5f, -25);

            // 4) Sala interior de Oxibot (fija en el mundo, tapa el exterior en esa zona)
            if (room != null)
            {
                var go = new GameObject("SalaOxibot");
                go.transform.SetParent(root);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = room;
                sr.sortingOrder = -15;
                Vector2 size = room.bounds.size;
                go.transform.position = new Vector3(BossRoomStartX + size.x * 0.5f, size.y * 0.5f, 0f);
            }
        }

        static void TiledLayer(string name, Sprite sprite, Transform parent, float levelWidth, float height,
                               float bottomY, float factorX, float factorY, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = new Vector2(levelWidth + 60f, height);
            sr.sortingOrder = order;
            go.transform.position = new Vector3(levelWidth * 0.5f, bottomY + height * 0.5f, 0f);
            var p = go.AddComponent<Kael.ParallaxLayer>();
            p.factorX = factorX;
            p.factorY = factorY;
        }

        static Sprite ImportSprite(string file)
        {
            string path = Folder + file;
            if (!File.Exists(path))
            {
                Debug.LogWarning("[Kael] Falta el fondo " + path);
                return null;
            }
            var ti = (TextureImporter)AssetImporter.GetAtPath(path);
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = PPU;
            ti.filterMode = FilterMode.Point;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.wrapMode = TextureWrapMode.Clamp;
            var settings = new TextureImporterSettings();
            ti.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteAlignment = (int)SpriteAlignment.Center;
            ti.SetTextureSettings(settings);
            ti.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static void DestroyIfExists(GameObject go)
        {
            if (go != null) Object.DestroyImmediate(go);
        }
    }
}
#endif
