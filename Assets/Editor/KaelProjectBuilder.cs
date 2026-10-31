#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

namespace Kael.EditorTools
{
    /// <summary>
    /// Construye automáticamente el prototipo del Avance 2:
    /// carpetas, layers, sprites placeholder (pixel art), tiles, prefabs, las 4 escenas y Build Settings.
    ///
    /// Uso: menú superior de Unity → Kael → Construir proyecto (Avance 2)
    ///
    /// - El nivel se lee de Assets/Levels/Nivel1.txt (se puede editar y volver a construir).
    /// - Los PNG de Assets/Sprites que ya existan NO se sobreescriben: si el equipo reemplaza un
    ///   placeholder por arte final con el mismo nombre (16 px por unidad), se conserva al reconstruir.
    /// - Las escenas y prefabs SÍ se regeneran cada vez.
    /// </summary>
    public static class KaelProjectBuilder
    {
        const string LevelFile = "Assets/Levels/Nivel1.txt";
        const int PPU = 16;

        static readonly string[] Folders =
        {
            "Assets/Scenes", "Assets/Prefabs", "Assets/Prefabs/Characters", "Assets/Prefabs/Enemies",
            "Assets/Prefabs/Level", "Assets/Prefabs/Projectiles", "Assets/Sprites", "Assets/Sprites/Characters",
            "Assets/Sprites/Enemies", "Assets/Sprites/Tiles", "Assets/Sprites/Items", "Assets/Sprites/Backgrounds",
            "Assets/Tiles", "Assets/Audio", "Assets/Audio/Music", "Assets/Audio/SFX", "Assets/Animations",
            "Assets/Materials", "Assets/Levels",
        };

        // Paleta (coincide con el GDD: ambiente gris industrial, Kael y jefes con colores llamativos)
        static readonly Dictionary<char, Color32> Pal = new Dictionary<char, Color32>
        {
            {'k', Hex("1a1c2c")}, {'H', Hex("6b3d1f")}, {'f', Hex("4a4a52")}, {'c', Hex("41e0f0")},
            {'S', Hex("f2c39a")}, {'O', Hex("e07a2b")}, {'o', Hex("a84f1a")}, {'B', Hex("2f4f86")},
            {'b', Hex("7a5a3a")}, {'w', Hex("ffffff")}, {'G', Hex("8a8f98")}, {'D', Hex("4b4f58")},
            {'L', Hex("c2c7cf")}, {'R', Hex("9c4a25")}, {'Y', Hex("ffe14a")}, {'E', Hex("5ff5ff")},
            {'r', Hex("d83a3a")}, {'P', Hex("e04ff0")}, {'N', Hex("4fd06a")}, {'s', Hex("5a3a22")},
        };

        static readonly Color UiGreen = Hex("1f3a2e");
        static readonly Color UiGreenDark = Hex("152820");
        static readonly Color UiRed = Hex("7a2e2e");
        static readonly Color CamBg = Hex("20242e");

        static int groundLayer, playerLayer, enemyLayer;
        static Font font;

        [MenuItem("Kael/Construir proyecto (Avance 2)", priority = 0)]
        public static void BuildAll()
        {
            if (!File.Exists(LevelFile))
            {
                EditorUtility.DisplayDialog("Kael", "No se encontró " + LevelFile + ".\nCopie la carpeta Levels dentro de Assets.", "OK");
                return;
            }
            if (!EditorUtility.DisplayDialog("Kael: Rebelión Mecánica",
                    "Se van a (re)crear las escenas MenuPrincipal, Nivel1, Victoria y Derrota, junto con los prefabs.\n\n" +
                    "Los cambios hechos a mano dentro de esas escenas se perderán. ¿Continuar?", "Construir", "Cancelar"))
                return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            try
            {
                EditorUtility.DisplayProgressBar("Kael", "Preparando proyecto...", 0.05f);
                CheckInputHandler();
                foreach (string f in Folders) EnsureFolder(f);
                groundLayer = EnsureLayer("Ground");
                playerLayer = EnsureLayer("Player");
                enemyLayer = EnsureLayer("Enemy");
                font = LoadBuiltinFont();

                EditorUtility.DisplayProgressBar("Kael", "Generando sprites...", 0.2f);
                var art = BuildSprites();

                EditorUtility.DisplayProgressBar("Kael", "Creando tiles y prefabs...", 0.4f);
                var tiles = BuildTiles(art);
                var prefabs = BuildPrefabs(art);

                EditorUtility.DisplayProgressBar("Kael", "Creando escenas...", 0.6f);
                string menu = BuildMenuScene(art);
                string level = BuildLevelScene(art, tiles, prefabs);
                string win = BuildEndScene(SceneNames.Victory, "¡NIVEL COMPLETADO!",
                    "Has logrado detener la rebelión de las máquinas.", "Volver a Jugar", Hex("4fd06a"));
                string lose = BuildEndScene(SceneNames.Defeat, "KAEL HA CAÍDO",
                    "Las máquinas siguen fuera de control...", "Reintentar", Hex("d83a3a"));

                EditorBuildSettings.scenes = new[]
                {
                    new EditorBuildSettingsScene(menu, true), new EditorBuildSettingsScene(level, true),
                    new EditorBuildSettingsScene(win, true), new EditorBuildSettingsScene(lose, true),
                };

                AssetDatabase.SaveAssets();
                EditorSceneManager.OpenScene(menu);
                Debug.Log("[Kael] Proyecto construido. Abra la escena MenuPrincipal y presione Play.");
                EditorUtility.DisplayDialog("Kael", "¡Listo! Se creó el prototipo.\n\nPresione Play en la escena MenuPrincipal.", "OK");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        // ───────────────────────────── Configuración ─────────────────────────────

        static void CheckInputHandler()
        {
            // Los scripts usan el Input Manager clásico. Si el proyecto solo tiene el Input System nuevo, se activa "Both".
            Object[] ps = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (ps == null || ps.Length == 0) return;
            var so = new SerializedObject(ps[0]);
            SerializedProperty p = so.FindProperty("activeInputHandler");
            if (p != null && p.intValue == 1)
            {
                p.intValue = 2;
                so.ApplyModifiedProperties();
                EditorUtility.DisplayDialog("Kael", "Se cambió 'Active Input Handling' a 'Both' para que funcione el Input Manager clásico.\n" +
                                                    "Reinicie Unity cuando termine la construcción.", "OK");
            }
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        static int EnsureLayer(string name)
        {
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty layers = tagManager.FindProperty("layers");
            for (int i = 0; i < layers.arraySize; i++)
                if (layers.GetArrayElementAtIndex(i).stringValue == name) return i;
            for (int i = 8; i < layers.arraySize; i++)
            {
                SerializedProperty sp = layers.GetArrayElementAtIndex(i);
                if (string.IsNullOrEmpty(sp.stringValue))
                {
                    sp.stringValue = name;
                    tagManager.ApplyModifiedProperties();
                    return i;
                }
            }
            Debug.LogError("[Kael] No hay espacio para crear el layer " + name);
            return 0;
        }

        // ───────────────────────────── Sprites ─────────────────────────────

        class Art
        {
            public Sprite kael, chatarra, voltio, electabot, oxibot, bullet, bolt, shockwave, health, weapon;
            public Sprite ground, platform, spikes, door, flag, background;
        }

        static Art BuildSprites()
        {
            var a = new Art();
            a.kael = PixelSprite("Characters/Kael", new[]
            {
                "....kkkkkk..", "...kHHHHHHk.", "..kHHHHHHHHk", "..kffffffffk", "..kfccffccfk", "..kSSSSSSSSk",
                "..kSSSkSSkSk", "..kSSSSSSSSk", "...kSSSSSSk.", ".kbkOOOOOOk.", "kbbkOOooOOSk", "kbbkOOooOOSk",
                ".kkkBBBBBBk.", "...kBBkkBBk.", "...kBBk.kBBk", "..kssk..kssk",
            });
            a.chatarra = PixelSprite("Enemies/Chatarra", new[]
            {
                "....kkkkkk..", "...kGGRGGGk.", "..kGGGGEEGk.", "..kGRGGEEGk.", "..kGGGGGGRk.", ".kDkkkkkkkDk",
                "kDGGRGGGRGDk", "kDGGGGRGGGDk", ".kkkkkkkkkk.", "..kDk..kDk..", "..kkk..kkk..",
            });
            a.voltio = PixelSprite("Enemies/Voltio", new[]
            {
                "...kkkk...", "..kYYYYk..", ".kYYwYYYk.", "kYYYYYEEYk", "kYYYYYEEYk", "kYYYYYYYYk",
                "kYRYYYYRYk", ".kYYYYYYk.", "..kkkkkk..", "..kD..kD..",
            });
            a.electabot = PixelSprite("Enemies/Electabot", new[]
            {
                "kkkkk..kkkkk", ".kLLLkkLLLk.", "....kDDk....", "..kkGGGGkk..", ".kGGGEEGGGk.", "kDGGGEEGGGDk",
                ".kDDDDDDDDk.", "..kYk..kYk..", "...Y....Y...",
            });
            a.bullet = PixelSprite("Items/Proyectil", new[] { "..wwww..", ".wwwwww.", "wwwwwwww", ".wwwwww." });
            a.bolt = PixelSprite("Items/Descarga", new[] { ".ww.", "wwww", ".ww.", "ww..", ".ww.", "..ww", ".ww.", "ww.." });
            a.shockwave = PixelSprite("Items/OndaChoque", new[]
            {
                "....ww....", "...wwww...", "..ww..ww..", ".ww....ww.", "ww......ww", "w........w",
            });
            a.health = PixelSprite("Items/Vida", new[]
            {
                "..kkkkkk..", ".kNNNNNNk.", "kNNNwwNNNk", "kNNNwwNNNk", "kNwwwwwwNk", "kNwwwwwwNk",
                "kNNNwwNNNk", "kNNNwwNNNk", ".kNNNNNNk.", "..kkkkkk..",
            });
            a.weapon = PixelSprite("Items/ArmaMejorada", new[]
            {
                "..kkkkkkkk....", ".kPPPPPPPPkkk.", "kPPwwPPPPPPPEk", "kPPPPPPPPPPPEk", ".kkkkPPPkkkkk.",
                "....kPPk......", "....kPPk......", "....kkkk......",
            });

            a.oxibot = ProcSprite("Enemies/OxibotGigante", 48, 48, DrawOxibot);
            a.ground = ProcSprite("Tiles/Suelo", 16, 16, DrawGround);
            a.platform = ProcSprite("Tiles/Plataforma", 16, 16, DrawPlatform);
            a.spikes = ProcSprite("Tiles/Espinas", 16, 16, DrawSpikes);
            a.door = ProcSprite("Tiles/Puerta", 16, 16, DrawDoor);
            a.flag = ProcSprite("Items/Meta", 16, 32, DrawFlag);
            a.background = ProcSprite("Backgrounds/Fabrica", 128, 64, DrawBackground);
            return a;
        }

        static Sprite PixelSprite(string name, string[] rows)
        {
            int h = rows.Length, w = 0;
            foreach (string r in rows) w = Mathf.Max(w, r.Length);
            var px = new Color32[w * h];
            for (int r = 0; r < h; r++)
                for (int x = 0; x < rows[r].Length; x++)
                    if (Pal.TryGetValue(rows[r][x], out Color32 c)) px[(h - 1 - r) * w + x] = c;
            return SaveSprite(name, w, h, px);
        }

        static Sprite ProcSprite(string name, int w, int h, System.Action<Color32[], int, int> draw)
        {
            var px = new Color32[w * h];
            draw(px, w, h);
            return SaveSprite(name, w, h, px);
        }

        static Sprite SaveSprite(string name, int w, int h, Color32[] px)
        {
            string path = "Assets/Sprites/" + name + ".png";
            if (!File.Exists(path)) // no sobreescribe arte que el equipo ya haya reemplazado
            {
                var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
                tex.SetPixels32(px);
                tex.Apply();
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }

            var ti = (TextureImporter)AssetImporter.GetAtPath(path);
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = PPU;
            ti.filterMode = FilterMode.Point;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            var settings = new TextureImporterSettings();
            ti.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteAlignment = (int)SpriteAlignment.Center;
            ti.SetTextureSettings(settings);
            ti.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        // --- Dibujos procedurales (coordenadas con y=0 abajo) ---

        static void Rect(Color32[] px, int w, int x0, int y0, int x1, int y1, Color32 c)
        {
            int h = px.Length / w;
            for (int y = Mathf.Max(0, y0); y <= Mathf.Min(h - 1, y1); y++)
                for (int x = Mathf.Max(0, x0); x <= Mathf.Min(w - 1, x1); x++)
                    px[y * w + x] = c;
        }

        static void DrawGround(Color32[] px, int w, int h)
        {
            Rect(px, w, 0, 0, 15, 15, Hex("4b4f58"));
            Rect(px, w, 1, 1, 14, 14, Hex("5d626c"));
            Rect(px, w, 1, 13, 14, 14, Hex("767c87"));
            Rect(px, w, 1, 1, 14, 2, Hex("444851"));
            foreach (var p in new[] { new Vector2Int(3, 11), new Vector2Int(12, 11), new Vector2Int(3, 4), new Vector2Int(12, 4) })
                Rect(px, w, p.x, p.y, p.x, p.y, Hex("9aa0aa"));
            Rect(px, w, 6, 6, 9, 7, Hex("9c4a25")); // óxido
        }

        static void DrawPlatform(Color32[] px, int w, int h)
        {
            Rect(px, w, 0, 11, 15, 15, Hex("1a1c2c"));
            for (int x = 0; x < 16; x++)
                Rect(px, w, x, 12, x, 14, ((x / 3) % 2 == 0) ? Hex("ffe14a") : Hex("2a2c38"));
            Rect(px, w, 2, 6, 3, 10, Hex("4b4f58"));
            Rect(px, w, 12, 6, 13, 10, Hex("4b4f58"));
        }

        static void DrawSpikes(Color32[] px, int w, int h)
        {
            for (int s = 0; s < 4; s++)
            {
                int cx = s * 4 + 2;
                for (int y = 0; y < 8; y++)
                {
                    int half = (8 - y) / 4 + (y < 4 ? 1 : 0);
                    Rect(px, w, cx - half, y, cx + half - 1, y, y > 5 ? Hex("e8ecf2") : Hex("a7adb8"));
                }
            }
        }

        static void DrawDoor(Color32[] px, int w, int h)
        {
            Rect(px, w, 0, 0, 15, 15, Hex("1a1c2c"));
            for (int y = 1; y < 15; y++)
                for (int x = 1; x < 15; x++)
                    px[y * w + x] = ((x + y) / 4) % 2 == 0 ? Hex("d83a3a") : Hex("2a2c38");
        }

        static void DrawFlag(Color32[] px, int w, int h)
        {
            Rect(px, w, 2, 0, 3, 31, Hex("c2c7cf"));
            Rect(px, w, 1, 0, 5, 1, Hex("4b4f58"));
            for (int y = 18; y < 30; y++)
            {
                int len = 11 - Mathf.Abs(y - 24);
                Rect(px, w, 4, y, 4 + len, y, Hex("4fd06a"));
            }
            Rect(px, w, 6, 23, 8, 25, Hex("ffffff"));
        }

        static void DrawOxibot(Color32[] px, int w, int h)
        {
            Color32 k = Hex("1a1c2c"), rust = Hex("9c4a25"), rustL = Hex("c06a35"), metal = Hex("6d7380"), core = Hex("5ff5ff");
            // piernas
            Rect(px, w, 10, 0, 19, 9, k); Rect(px, w, 11, 1, 18, 8, metal);
            Rect(px, w, 26, 0, 35, 9, k); Rect(px, w, 27, 1, 34, 8, metal);
            // cuerpo
            Rect(px, w, 6, 8, 38, 34, k); Rect(px, w, 7, 9, 37, 33, rust);
            Rect(px, w, 7, 30, 37, 33, rustL);
            Rect(px, w, 10, 12, 14, 14, metal); Rect(px, w, 30, 12, 34, 14, metal);
            // núcleo de energía en el pecho
            Rect(px, w, 18, 17, 26, 25, k); Rect(px, w, 19, 18, 25, 24, core); Rect(px, w, 20, 22, 21, 23, Hex("ffffff"));
            // cabeza
            Rect(px, w, 13, 34, 31, 46, k); Rect(px, w, 14, 35, 30, 45, metal);
            Rect(px, w, 16, 39, 20, 42, Hex("ffe14a")); Rect(px, w, 24, 39, 28, 42, Hex("ffe14a"));
            Rect(px, w, 16, 36, 28, 37, k);
            // brazo gigante (lado derecho)
            Rect(px, w, 37, 6, 47, 32, k); Rect(px, w, 38, 7, 46, 31, metal);
            Rect(px, w, 38, 7, 46, 12, rustL);
            Rect(px, w, 0, 14, 7, 30, k); Rect(px, w, 1, 15, 6, 29, metal);
        }

        static void DrawBackground(Color32[] px, int w, int h)
        {
            Color32 far = Hex("2c313d"), near = Hex("353b48"), win = Hex("5a5230");
            Rect(px, w, 0, 0, 127, 10, near);
            Rect(px, w, 4, 0, 30, 40, far); Rect(px, w, 10, 40, 14, 58, far); Rect(px, w, 20, 40, 23, 52, far);
            Rect(px, w, 36, 0, 70, 30, near); Rect(px, w, 60, 30, 64, 50, near);
            Rect(px, w, 78, 0, 110, 46, far); Rect(px, w, 96, 46, 101, 62, far);
            Rect(px, w, 112, 0, 127, 24, near);
            for (int x = 8; x < 28; x += 6) for (int y = 14; y < 36; y += 8) Rect(px, w, x, y, x + 2, y + 3, win);
            for (int x = 82; x < 108; x += 7) for (int y = 12; y < 42; y += 9) Rect(px, w, x, y, x + 3, y + 3, win);
        }

        // ───────────────────────────── Tiles ─────────────────────────────

        class TileSet { public Tile ground, platform; }

        static TileSet BuildTiles(Art art)
        {
            return new TileSet
            {
                ground = SaveTile("Assets/Tiles/Suelo.asset", art.ground),
                platform = SaveTile("Assets/Tiles/Plataforma.asset", art.platform),
            };
        }

        static Tile SaveTile(string path, Sprite sprite)
        {
            Tile t = AssetDatabase.LoadAssetAtPath<Tile>(path);
            if (t == null)
            {
                t = ScriptableObject.CreateInstance<Tile>();
                AssetDatabase.CreateAsset(t, path);
            }
            t.sprite = sprite;
            t.colliderType = Tile.ColliderType.Grid;
            EditorUtility.SetDirty(t);
            return t;
        }

        // ───────────────────────────── Prefabs ─────────────────────────────

        class Prefabs
        {
            public GameObject player, bullet, bolt, shockwave, chatarra, voltio, electabot, oxibot;
            public GameObject spikes, healthPickup, weaponPickup, goal;
        }

        static Prefabs BuildPrefabs(Art art)
        {
            var p = new Prefabs();
            int groundMask = 1 << groundLayer;

            var noFriction = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>("Assets/Materials/SinFriccion.physicsMaterial2D");
            if (noFriction == null)
            {
                noFriction = new PhysicsMaterial2D("SinFriccion") { friction = 0f, bounciness = 0f };
                AssetDatabase.CreateAsset(noFriction, "Assets/Materials/SinFriccion.physicsMaterial2D");
            }

            // Proyectiles
            p.bullet = SavePrefab(MakeProjectile("Proyectil_Kael", art.bullet, Team.Player, 1.5f, true, groundMask, 0.15f), "Projectiles/Proyectil_Kael");
            p.bolt = SavePrefab(MakeProjectile("Descarga_Electabot", art.bolt, Team.Enemy, 3f, true, groundMask, 0.15f), "Projectiles/Descarga_Electabot");
            var sw = MakeProjectile("OndaChoque_Oxibot", art.shockwave, Team.Enemy, 2.5f, false, groundMask, 0f);
            var swCol = sw.AddComponent<BoxCollider2D>(); swCol.isTrigger = true; swCol.size = new Vector2(0.6f, 0.4f);
            p.shockwave = SavePrefab(sw, "Projectiles/OndaChoque_Oxibot");

            // Kael
            {
                var go = SpriteObject("Kael", art.kael, 5);
                go.tag = "Player";
                go.layer = playerLayer;
                var rb = Body(go, 3f, 1f);
                rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                rb.interpolation = RigidbodyInterpolation2D.Interpolate;
                var col = go.AddComponent<CapsuleCollider2D>();
                col.size = new Vector2(0.6f, 0.95f);
                col.sharedMaterial = noFriction;
                var hp = go.AddComponent<Health>();
                hp.team = Team.Player; hp.maxHealth = 5; hp.invulnerableTime = 1f; hp.destroyOnDeath = false; hp.scoreValue = 0;
                var pc = go.AddComponent<PlayerController>();
                pc.groundLayer = groundMask;
                var sh = go.AddComponent<PlayerShooter>();
                sh.projectilePrefab = p.bullet.GetComponent<Projectile>();
                p.player = SavePrefab(go, "Characters/Kael");
            }

            // Enemigos
            {
                var go = EnemyObject("Chatarra", art.chatarra, 1, 100, groundMask, new Vector2(0.7f, 0.6f));
                go.AddComponent<EnemyPatrol>().groundLayer = groundMask;
                p.chatarra = SavePrefab(go, "Enemies/Chatarra");
            }
            {
                var go = EnemyObject("Voltio", art.voltio, 2, 150, groundMask, new Vector2(0.55f, 0.55f));
                go.AddComponent<EnemyCharger>().groundLayer = groundMask;
                p.voltio = SavePrefab(go, "Enemies/Voltio");
            }
            {
                var go = EnemyObject("Electabot", art.electabot, 2, 200, groundMask, new Vector2(0.7f, 0.45f));
                go.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
                var d = go.AddComponent<EnemyDrone>();
                d.groundLayer = groundMask;
                d.boltPrefab = p.bolt.GetComponent<Projectile>();
                p.electabot = SavePrefab(go, "Enemies/Electabot");
            }

            // Objetos
            {
                var go = SpriteObject("Espinas", art.spikes, 1);
                var c = go.AddComponent<BoxCollider2D>(); c.isTrigger = true; c.size = new Vector2(0.9f, 0.4f); c.offset = new Vector2(0f, -0.25f);
                var d = go.AddComponent<DamageOnContact>(); d.team = Team.Enemy; d.damage = 1;
                p.spikes = SavePrefab(go, "Level/Espinas");
            }
            {
                var go = SpriteObject("Vida", art.health, 3);
                go.AddComponent<CircleCollider2D>().isTrigger = true;
                go.AddComponent<HealthPickup>();
                p.healthPickup = SavePrefab(go, "Level/Vida");
            }
            {
                var go = SpriteObject("ArmaMejorada", art.weapon, 3);
                var c = go.AddComponent<BoxCollider2D>(); c.isTrigger = true; c.size = new Vector2(1f, 0.7f);
                go.AddComponent<WeaponPickup>();
                p.weaponPickup = SavePrefab(go, "Level/ArmaMejorada");
            }
            {
                var go = SpriteObject("Meta", art.flag, 2);
                var c = go.AddComponent<BoxCollider2D>(); c.isTrigger = true; c.size = new Vector2(1f, 2f);
                go.AddComponent<LevelGoal>();
                p.goal = SavePrefab(go, "Level/Meta");
            }

            // Jefe
            {
                var go = EnemyObject("OxibotGigante", art.oxibot, 30, 1000, groundMask, new Vector2(2.4f, 2.8f));
                go.GetComponent<Rigidbody2D>().mass = 50f;
                go.GetComponent<Health>().destroyOnDeath = false;
                var b = go.AddComponent<BossOxibot>();
                b.groundLayer = groundMask;
                b.shockwavePrefab = p.shockwave.GetComponent<Projectile>();
                b.dropOnDeath = p.weaponPickup;
                p.oxibot = SavePrefab(go, "Enemies/OxibotGigante");
            }
            return p;
        }

        static GameObject SpriteObject(string name, Sprite sprite, int order)
        {
            var go = new GameObject(name);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            return go;
        }

        static Rigidbody2D Body(GameObject go, float gravity, float mass)
        {
            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = gravity;
            rb.mass = mass;
            rb.freezeRotation = true;
            return rb;
        }

        static GameObject EnemyObject(string name, Sprite sprite, int hp, int score, int groundMask, Vector2 colliderSize)
        {
            var go = SpriteObject(name, sprite, 4);
            go.layer = enemyLayer;
            Body(go, 3f, 1f);
            // Cápsula: se desliza sobre las uniones entre tiles sin trabarse
            var cap = go.AddComponent<CapsuleCollider2D>();
            cap.size = colliderSize;
            cap.direction = colliderSize.x > colliderSize.y ? CapsuleDirection2D.Horizontal : CapsuleDirection2D.Vertical;
            var h = go.AddComponent<Health>();
            h.team = Team.Enemy; h.maxHealth = hp; h.scoreValue = score; h.invulnerableTime = 0f;
            var d = go.AddComponent<DamageOnContact>(); d.team = Team.Enemy; d.damage = 1;
            return go;
        }

        static GameObject MakeProjectile(string name, Sprite sprite, Team team, float lifetime, bool destroyOnGround, int groundMask, float radius)
        {
            var go = SpriteObject(name, sprite, 6);
            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            if (radius > 0f)
            {
                var c = go.AddComponent<CircleCollider2D>();
                c.isTrigger = true;
                c.radius = radius;
            }
            var pr = go.AddComponent<Projectile>();
            pr.team = team; pr.lifetime = lifetime; pr.destroyOnGround = destroyOnGround; pr.groundLayer = groundMask;
            return go;
        }

        static GameObject SavePrefab(GameObject go, string relPath)
        {
            string path = "Assets/Prefabs/" + relPath + ".prefab";
            GameObject asset = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return asset;
        }

        // ───────────────────────────── Escena del nivel ─────────────────────────────

        static string BuildLevelScene(Art art, TileSet tiles, Prefabs pf)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            string[] rows = File.ReadAllText(LevelFile).Replace("\r", "").Split('\n');
            var lines = new List<string>();
            foreach (string r in rows) if (r.Length > 0) lines.Add(r);
            int H = lines.Count, W = 0;
            foreach (string l in lines) W = Mathf.Max(W, l.Length);

            // Cámara
            var cam = MakeCamera(7f);
            var follow = cam.gameObject.AddComponent<CameraFollow>();
            follow.minBounds = new Vector2(0f, 0f);
            follow.maxBounds = new Vector2(W, H);

            new GameObject("GameManager").AddComponent<GameManager>();
            new GameObject("AudioManager").AddComponent<AudioManager>();

            // Fondo por capas según el GDD (exterior industrial con parallax + sala de Oxibot)
            KaelBackgroundBuilder.CreateBackgrounds(W, H, cam);

            // Tilemaps
            var grid = new GameObject("Grid").AddComponent<Grid>();
            Tilemap ground = MakeTilemap(grid.transform, "Suelo", 0, false);
            Tilemap platforms = MakeTilemap(grid.transform, "Plataformas", 0, true);

            var level = new GameObject("Nivel").transform;
            var enemies = new GameObject("Enemigos").transform;
            var items = new GameObject("Objetos").transform;
            var bosses = new List<BossOxibot>();
            var doorCells = new List<Vector2Int>();
            GameObject player = null;

            for (int r = 0; r < H; r++)
            {
                int y = H - 1 - r;
                for (int x = 0; x < lines[r].Length; x++)
                {
                    char ch = lines[r][x];
                    switch (ch)
                    {
                        case '#': ground.SetTile(new Vector3Int(x, y, 0), tiles.ground); break;
                        case '=': platforms.SetTile(new Vector3Int(x, y, 0), tiles.platform); break;
                        case 'D': doorCells.Add(new Vector2Int(x, y)); break;
                        case 'P': player = Place(pf.player, x, y, null, true); break;
                        case 'C': Place(pf.chatarra, x, y, enemies, true); break;
                        case 'V': Place(pf.voltio, x, y, enemies, true); break;
                        case 'E': Place(pf.electabot, x, y, enemies, false); break;
                        case 'B': bosses.Add(Place(pf.oxibot, x, y, enemies, true).GetComponent<BossOxibot>()); break;
                        case '^': Place(pf.spikes, x, y, level, false); break;
                        case 'H': Place(pf.healthPickup, x, y, items, false); break;
                        case 'G': Place(pf.goal, x, y, items, true); break;
                    }
                }
            }
            ground.CompressBounds();
            platforms.CompressBounds();

            // Puertas (columnas de 'D' → un solo objeto sólido por columna)
            var doors = new List<GameObject>();
            doorCells.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));
            for (int i = 0; i < doorCells.Count;)
            {
                int j = i;
                while (j + 1 < doorCells.Count && doorCells[j + 1].x == doorCells[i].x && doorCells[j + 1].y == doorCells[j].y + 1) j++;
                int len = j - i + 1;
                var door = SpriteObject("Puerta_Jefe", art.door, 1);
                door.layer = groundLayer;
                door.transform.SetParent(level);
                door.transform.position = new Vector3(doorCells[i].x + 0.5f, doorCells[i].y + len * 0.5f, 0f);
                var sr = door.GetComponent<SpriteRenderer>();
                sr.drawMode = SpriteDrawMode.Tiled;
                sr.size = new Vector2(1f, len);
                door.AddComponent<BoxCollider2D>().size = new Vector2(1f, len);
                doors.Add(door);
                i = j + 1;
            }
            foreach (BossOxibot b in bosses)
            {
                b.doorsToOpen = doors.ToArray();
                PrefabUtility.RecordPrefabInstancePropertyModifications(b);
            }

            // Zona de muerte bajo el nivel
            var kill = new GameObject("ZonaMuerte");
            kill.transform.position = new Vector3(W * 0.5f, -4f, 0f);
            var kc = kill.AddComponent<BoxCollider2D>(); kc.isTrigger = true; kc.size = new Vector2(W + 40f, 2f);
            kill.AddComponent<KillZone>();

            if (player != null) follow.target = player.transform;
            else Debug.LogWarning("[Kael] El mapa no tiene 'P' (posición inicial de Kael).");

            BuildHUD();

            string path = "Assets/Scenes/" + SceneNames.Level1 + ".unity";
            EditorSceneManager.SaveScene(scene, path);
            return path;
        }

        static Tilemap MakeTilemap(Transform grid, string name, int order, bool oneWay)
        {
            var go = new GameObject(name);
            go.transform.SetParent(grid);
            go.layer = groundLayer;
            var tm = go.AddComponent<Tilemap>();
            go.AddComponent<TilemapRenderer>().sortingOrder = order;
            // Colisión directa por tile (sin CompositeCollider2D: su geometría no siempre se genera
            // antes de guardar la escena desde un script de editor y el suelo quedaba sin colisión).
            var tc = go.AddComponent<TilemapCollider2D>();
            if (oneWay)
            {
                tc.usedByEffector = true;
                var eff = go.AddComponent<PlatformEffector2D>();
                eff.useOneWay = true;
                eff.surfaceArc = 160f;
            }
            return tm;
        }

        /// <summary>Coloca un prefab en la celda (x,y). Si onGround, lo apoya sobre el borde inferior de la celda.</summary>
        static GameObject Place(GameObject prefab, int x, int y, Transform parent, bool onGround)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            if (parent != null) go.transform.SetParent(parent);
            float halfH = 0.5f;
            var sr = go.GetComponent<SpriteRenderer>();
            if (onGround && sr != null && sr.sprite != null) halfH = sr.sprite.bounds.extents.y;
            go.transform.position = new Vector3(x + 0.5f, y + halfH + (onGround ? 0.01f : 0f), 0f);
            return go;
        }

        static Camera MakeCamera(float size)
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            go.transform.position = new Vector3(0f, 0f, -10f);
            var cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = size;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = CamBg;
            go.AddComponent<AudioListener>();
            return cam;
        }

        // ───────────────────────────── UI ─────────────────────────────

        static Canvas MakeCanvas(string name)
        {
            var go = new GameObject(name);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();

            if (Object.FindObjectOfType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            return canvas;
        }

        static RectTransform UiObject(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        static RectTransform Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = anchor; rt.anchorMax = anchor; rt.pivot = pivot;
            rt.anchoredPosition = pos; rt.sizeDelta = size;
            return rt;
        }

        static RectTransform Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            return rt;
        }

        static Image Panel(string name, Transform parent, Color color)
        {
            var img = UiObject(name, parent).gameObject.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        static Text Label(string name, Transform parent, string text, int size, Color color, TextAnchor align = TextAnchor.MiddleCenter)
        {
            var t = UiObject(name, parent).gameObject.AddComponent<Text>();
            t.font = font;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        static Button MakeButton(string name, Transform parent, string text, Vector2 pos, Vector2 size)
        {
            var rt = Place(UiObject(name, parent), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, size);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = UiRed;
            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            ColorBlock cb = btn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.4f, 1.2f, 0.9f);
            cb.selectedColor = new Color(1.4f, 1.2f, 0.9f);
            cb.pressedColor = new Color(0.7f, 0.7f, 0.7f);
            cb.colorMultiplier = 1.5f;
            btn.colors = cb;

            // Borde inferior estilo 8 bits
            var shadow = rt.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.6f);
            shadow.effectDistance = new Vector2(4f, -4f);

            Stretch(Label("Texto", rt, text, 34, Color.white).rectTransform);
            return btn;
        }

        static void BuildHUD()
        {
            Canvas canvas = MakeCanvas("HUD");
            var hud = canvas.gameObject.AddComponent<HUDController>();
            Transform root = canvas.transform;

            // Destello de daño (pantalla completa)
            var flash = Panel("DestelloDaño", root, new Color(1f, 0f, 0f, 0f));
            Stretch(flash.rectTransform);
            hud.damageFlash = flash;

            // Vida de Kael (arriba a la izquierda)
            var hpLabel = Label("VidaTexto", root, "VIDA 5/5", 30, Color.white, TextAnchor.MiddleLeft);
            Place(hpLabel.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -24f), new Vector2(400f, 40f));
            var hpBg = Panel("VidaFondo", root, Hex("1a1c2c"));
            Place(hpBg.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -68f), new Vector2(380f, 30f));
            var hpFill = Panel("VidaRelleno", hpBg.transform, Hex("4fd06a"));
            Stretch(hpFill.rectTransform);
            hud.healthFill = hpFill.rectTransform;
            hud.healthText = hpLabel;

            // Puntaje (arriba al centro)
            var score = Label("Puntaje", root, "PUNTOS 00000", 34, Hex("ffe14a"));
            Place(score.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(500f, 50f));
            hud.scoreText = score;

            // Arma equipada (abajo a la izquierda)
            var iconBg = Panel("ArmaMarco", root, Hex("1a1c2c"));
            Place(iconBg.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(40f, 40f), new Vector2(64f, 64f));
            var icon = Panel("ArmaIcono", iconBg.transform, Hex("41e0f0"));
            Place(icon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(40f, 40f));
            var weaponText = Label("ArmaTexto", root, "Pistola de energía", 28, Color.white, TextAnchor.MiddleLeft);
            Place(weaponText.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(120f, 52f), new Vector2(500f, 40f));
            hud.weaponIcon = icon;
            hud.weaponText = weaponText;

            // Barra del jefe (arriba a la derecha, oculta hasta la pelea)
            var bossPanel = UiObject("PanelJefe", root);
            Place(bossPanel, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-40f, -24f), new Vector2(520f, 90f));
            var bossName = Label("JefeNombre", bossPanel, "OXIBOT GIGANTE", 30, Hex("ff7a7a"), TextAnchor.MiddleRight);
            Place(bossName.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero, new Vector2(520f, 40f));
            var bossBg = Panel("JefeFondo", bossPanel, Hex("1a1c2c"));
            Place(bossBg.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -44f), new Vector2(520f, 30f));
            var bossFill = Panel("JefeRelleno", bossBg.transform, Hex("d83a3a"));
            Stretch(bossFill.rectTransform);
            hud.bossPanel = bossPanel.gameObject;
            hud.bossNameText = bossName;
            hud.bossFill = bossFill.rectTransform;

            // Menú de pausa
            var pause = canvas.gameObject.AddComponent<PauseMenu>();
            var dim = Panel("PanelPausa", root, new Color(0f, 0f, 0f, 0.65f));
            dim.raycastTarget = true;
            Stretch(dim.rectTransform);
            var box = Panel("Caja", dim.transform, UiGreen);
            Place(box.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(620f, 560f));
            box.gameObject.AddComponent<Outline>().effectColor = Hex("1a1c2c");
            var title = Label("Titulo", box.transform, "PAUSA", 56, Color.white);
            Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(600f, 80f));
            pause.panel = dim.gameObject;
            pause.resumeButton = MakeButton("BtnContinuar", box.transform, "Continuar", new Vector2(0f, 50f), new Vector2(400f, 80f));
            pause.restartButton = MakeButton("BtnReiniciar", box.transform, "Reiniciar Nivel", new Vector2(0f, -60f), new Vector2(400f, 80f));
            pause.menuButton = MakeButton("BtnMenu", box.transform, "Menú Principal", new Vector2(0f, -170f), new Vector2(400f, 80f));
        }

        // ───────────────────────────── Menú principal ─────────────────────────────

        static string BuildMenuScene(Art art)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            MakeCamera(5f);
            new GameObject("AudioManager").AddComponent<AudioManager>();

            Canvas canvas = MakeCanvas("Canvas_Menu");
            Transform root = canvas.transform;
            Stretch(Panel("Fondo", root, UiGreen).rectTransform);

            var frame = Panel("Marco", root, new Color(0f, 0f, 0f, 0f));
            Stretch(frame.rectTransform);
            frame.rectTransform.offsetMin = new Vector2(30f, 30f);
            frame.rectTransform.offsetMax = new Vector2(-30f, -30f);
            var outline = frame.gameObject.AddComponent<Outline>();
            outline.effectColor = Hex("3b6fb0");
            outline.effectDistance = new Vector2(3f, 3f);

            var title = Label("Titulo", root, "KAEL: REBELIÓN MECÁNICA", 84, Color.white);
            Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -120f), new Vector2(1600f, 120f));
            title.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(6f, -6f);
            var sub = Label("Subtitulo", root, "Prototipo - Avance 2", 30, Hex("ffe14a"));
            Place(sub.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -230f), new Vector2(1000f, 50f));

            // Kael decorando el menú
            var kael = UiObject("Kael", root).gameObject.AddComponent<Image>();
            kael.sprite = art.kael;
            kael.preserveAspect = true;
            kael.raycastTarget = false;
            Place(kael.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-520f, -60f), new Vector2(240f, 320f));

            var menu = canvas.gameObject.AddComponent<MainMenuController>();
            menu.playButton = MakeButton("BtnJugar", root, "Jugar", new Vector2(0f, 20f), new Vector2(460f, 100f));
            menu.controlsButton = MakeButton("BtnControles", root, "Controles", new Vector2(0f, -120f), new Vector2(460f, 100f));
            menu.quitButton = MakeButton("BtnSalir", root, "Salir", new Vector2(0f, -260f), new Vector2(460f, 100f));

            // Panel de controles
            var cp = Panel("PanelControles", root, new Color(0f, 0f, 0f, 0.8f));
            cp.raycastTarget = true;
            Stretch(cp.rectTransform);
            var box = Panel("Caja", cp.transform, UiGreenDark);
            Place(box.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1100f, 720f));
            var ct = Label("Titulo", box.transform, "CONTROLES", 56, Color.white);
            Place(ct.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(1000f, 80f));
            var body = Label("Texto", box.transform,
                "Moverse:   A / D   o   flechas izquierda / derecha\n\n" +
                "Saltar:   Espacio,  W  o  flecha arriba\n(mantener presionado = salto más alto)\n\n" +
                "Disparar:   J,  K,  Ctrl izquierdo  o  clic izquierdo\n\n" +
                "Pausa:   Esc  o  P", 34, Color.white);
            Place(body.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(1000f, 460f));
            menu.controlsPanel = cp.gameObject;
            menu.closeControlsButton = MakeButton("BtnVolver", box.transform, "Volver", new Vector2(0f, -290f), new Vector2(360f, 80f));

            string path = "Assets/Scenes/" + SceneNames.Menu + ".unity";
            EditorSceneManager.SaveScene(scene, path);
            return path;
        }

        // ───────────────────────────── Gane / Pérdida ─────────────────────────────

        static string BuildEndScene(string sceneName, string titleText, string message, string retryText, Color accent)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            MakeCamera(5f);
            new GameObject("AudioManager").AddComponent<AudioManager>();

            Canvas canvas = MakeCanvas("Canvas_" + sceneName);
            Transform root = canvas.transform;
            Stretch(Panel("Fondo", root, UiGreen).rectTransform);

            var title = Label("Titulo", root, titleText, 90, accent);
            Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(1600f, 130f));
            title.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(6f, -6f);
            var msg = Label("Mensaje", root, message, 40, Color.white);
            Place(msg.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -290f), new Vector2(1400f, 120f));

            var end = canvas.gameObject.AddComponent<EndScreenController>();
            var score = Label("Puntaje", root, "PUNTAJE: 00000", 44, Hex("ffe14a"));
            Place(score.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(900f, 70f));
            end.scoreText = score;
            end.retryButton = MakeButton("BtnReintentar", root, retryText, new Vector2(0f, -110f), new Vector2(460f, 100f));
            end.menuButton = MakeButton("BtnMenu", root, "Menú Principal", new Vector2(0f, -250f), new Vector2(460f, 100f));

            string path = "Assets/Scenes/" + sceneName + ".unity";
            EditorSceneManager.SaveScene(scene, path);
            return path;
        }

        // ───────────────────────────── Utilidades ─────────────────────────────

        static Font LoadBuiltinFont()
        {
            // Unity 2022.2+ usa LegacyRuntime.ttf; versiones anteriores usan Arial.ttf
            try { Font f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); if (f != null) return f; } catch { }
            try { return Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch { return null; }
        }

        static Color32 Hex(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out Color c);
            return c;
        }
    }
}
#endif
