using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static OZLobbyUI;

[RequireComponent(typeof(OZAccount))]
public sealed class OZLobby : MonoBehaviour
{
    public OZGame game;
    public OZLobbyStage stage;
    public Font regular, bold, italic;
    public TextAsset credits, fontLicense;
    public Sprite panelSprite, goldSprite, brand, skins, weapons, kits, news, playIcon;

    readonly string[] modes = { "BATTLE ROYALE", "COMBATE", "TREINAMENTO" };
    readonly string[] modeDetails = { "Solo • 12 bots", "Solo • 4 bots", "Prática offline" };

    OZAccount account;
    OZLobbyInventory inventory;
    OZLobbyUI ui;
    Canvas canvas;
    RectTransform safe, dialog;
    Text accountHeading, status, cash, currentMode, modeDetail, ranking, rememberedLabel, userTitle;
    InputField user, password;
    Button login, register, signOut, play;
    bool remember, savedSession, revealPassword;
    Text passwordVisibility;
    Rect lastSafe;
    Vector2Int lastScreen;
    OZGame.Mode mode;
    string server, startupError;
    bool renderingLobby;
    string shownResult;
    bool loadedOutfit;
    int portraitPointer = int.MinValue;
    int shownBalance = -1;

    void Start()
    {
        account = GetComponent<OZAccount>();
        server = PlayerPrefs.GetString("OZ_SERVER", "");
        remember = PlayerPrefs.HasKey("OZ_LOGIN_NAME");
        inventory = new OZLobbyInventory();
        ui = new OZLobbyUI(regular, bold, italic, panelSprite, goldSprite);
        CreateCanvas();
        BuildLobby();
        if (stage) stage.Apply(inventory);
        Update();
    }

    void CreateCanvas()
    {
        var screen = new GameObject("Lobby V0.7", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster));
        screen.transform.SetParent(transform, false);
        canvas = screen.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 12;
        var scaler = screen.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        safe = Layer(screen.transform, "Área segura");

        if (!FindAnyObjectByType<EventSystem>())
        {
            var events = new GameObject("Eventos do lobby", typeof(EventSystem), typeof(StandaloneInputModule));
            events.transform.SetParent(transform, false);
        }
    }

    void BuildLobby()
    {
        BuildPortraitControls();
        Picture(Place(safe, "Operação Zero", 23, 42, 308, 95), brand, Color.white);
        var tag = Place(safe, "Versão de teste", 115, 146, 155, 34);
        Picture(tag, goldSprite, Color.white, slice: true);
        ui.Label(tag, "V0.7 DEV", 8, 0, 139, 34, 25, Ink, TextAnchor.MiddleCenter, slanted: true);

        var nav = Layer(safe, "Navegação principal");
        nav.anchorMin = new Vector2(0, 1);
        nav.anchorMax = new Vector2(1, 1);
        nav.pivot = new Vector2(.5f, 1);
        nav.offsetMin = new Vector2(344, -48);
        nav.offsetMax = new Vector2(-12, 0);
        Picture(nav, panelSprite, Color.white, slice: true);
        ui.Button(nav, "JOGAR", 0, 0, 134, 47, CloseDialog, true);
        ui.Button(nav, "ARSENAL", 136, 0, 120, 47, OpenArsenal, bare: true);
        ui.Button(nav, "OPERADOR", 258, 0, 128, 47, OpenOperator, bare: true);
        ui.Button(nav, "LOJA", 389, 0, 94, 47, () => OpenStore(OZLobbyInventory.Category.Outfit), bare: true);
        ui.Button(nav, "CARREIRA", 486, 0, 122, 47, OpenCareer, bare: true);
        ui.Button(nav, "AJUSTES", 612, 0, 122, 47, OpenSettings, bare: true);
        var identity = Place(nav, "Perfil", -164, 0, 150, 47, RightTop);
        userTitle = ui.Label(identity, "CONVIDADO", 0, 0, 148, 47, 18, Gold, TextAnchor.MiddleRight, strong: true);

        BuildAccount();
        BuildShop();

        var notes = ui.Panel(safe, "Atualização", 23, -132, 320, 103, LeftBottom);
        Picture(Place(notes, "Costa de Ferro", 8, 8, 105, 86), news, Color.white);
        ui.Label(notes, "ATUALIZAÇÃO V0.7", 123, 6, 188, 27, 23, Gold, slanted: true);
        ui.Label(notes, "Rotação, câmera e controles\nCosta de Ferro", 123, 34, 180, 53, 19, Muted);
        ui.Button(notes, "", 2, 2, 316, 99, OpenNotes, bare: true);

        var selector = ui.Button(safe, "", -338, -178, 312, 54, OpenModes, anchor: RightBottom);
        currentMode = ui.Label(selector.transform, modes[0], 20, 1, 248, 30, 24, slanted: true);
        modeDetail = ui.Label(selector.transform, modeDetails[0], 20, 29, 250, 22, 17, Muted);
        ui.Label(selector.transform, "›", 269, 1, 26, 52, 38, Gold, TextAnchor.MiddleCenter);
        play = ui.Button(safe, "", -343, -113, 320, 83, Deploy, true, anchor: RightBottom);
        Picture(Place(play.transform, "Ícone jogar", 51, 22, 41, 41), playIcon, Ink);
        ui.Label(play.transform, "JOGAR", 90, 0, 172, 83, 52, Ink, TextAnchor.MiddleCenter, slanted: true);
    }

    void BuildPortraitControls()
    {
        if (!stage || !stage.operatorRoot) return;
        // Sibling behind all navigation and dialog controls; only the central space receives drag.
        var region = Layer(safe, "Girar operador 3D");
        region.anchorMin = new Vector2(.5f, .5f);
        region.anchorMax = region.anchorMin;
        region.pivot = new Vector2(.5f, .5f);
        region.sizeDelta = new Vector2(480, 470);
        region.anchoredPosition = new Vector2(0, -5);
        Picture(region, null, Invisible, hit: true);
        var events = region.gameObject.AddComponent<EventTrigger>();
        void Bind(EventTriggerType type, Action<PointerEventData> callback)
        {
            var entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener(data => { if (data is PointerEventData pointer) callback(pointer); });
            events.triggers.Add(entry);
        }
        Bind(EventTriggerType.BeginDrag, p =>
        {
            if (!dialog && portraitPointer == int.MinValue) portraitPointer = p.pointerId;
        });
        Bind(EventTriggerType.Drag, p =>
        {
            if (portraitPointer == p.pointerId && !dialog && stage)
                stage.RotateOperator(-p.delta.x / Mathf.Max(.01f, canvas.scaleFactor) * .75f);
        });
        Bind(EventTriggerType.EndDrag, p => { if (portraitPointer == p.pointerId) portraitPointer = int.MinValue; });
        Bind(EventTriggerType.PointerUp, p => { if (portraitPointer == p.pointerId) portraitPointer = int.MinValue; });
        var hint = Place(safe, "Orientação do operador", -135, -72, 270, 42, new Vector2(.5f, 0));
        ui.Label(hint, "ARRASTE PARA GIRAR • 360°", 0, 0, 270, 42, 18, Muted, TextAnchor.MiddleCenter);
        ui.Button(safe, "CENTRALIZAR", -78, -119, 156, 38, stage.ResetOrientation, size: 17, anchor: new Vector2(.5f, 0));
    }

    void OnApplicationFocus(bool focus)
    {
        if (!focus) portraitPointer = int.MinValue;
    }

    void BuildAccount()
    {
        var panel = ui.Panel(safe, "Conta", 23, 194, 312, 332);
        accountHeading = ui.Label(panel, "LOGIN / CONTA", 23, 10, 260, 35, 28, strong: true);
        status = ui.Label(panel, "", 24, 43, 264, 36, 16, Muted);
        user = ui.Input(panel, "Usuário", PlayerPrefs.GetString("OZ_LOGIN_NAME", ""), 24, 83, 264, 42,
            InputField.ContentType.Standard, 22);
        password = ui.Input(panel, "Senha", "", 24, 131, 264, 42, InputField.ContentType.Password, 128);
        password.textComponent.rectTransform.sizeDelta = new Vector2(172, 42);
        ((RectTransform)password.placeholder.transform).sizeDelta = new Vector2(172, 42);
        var visibility = ui.Button(password.transform, "", 189, 0, 71, 42,
            () => SetPasswordVisible(!revealPassword), bare: true);
        passwordVisibility = ui.Label(visibility.transform, "VER", 0, 0, 71, 42, 17, Gold,
            TextAnchor.MiddleCenter, strong: true);
        var saveUser = ui.Button(panel, "", 24, 178, 264, 37, ToggleRemember, bare: true);
        rememberedLabel = ui.Label(saveUser.transform, "", 1, 0, 259, 37, 18, Gold);
        UpdateRememberLabel();

        login = ui.Button(panel, "ENTRAR  ›", 24, 219, 264, 53, () => Authenticate(false), true, 27);
        register = ui.Button(panel, "CRIAR CONTA", 24, 279, 264, 43, () => Authenticate(true));
        signOut = ui.Button(panel, "SAIR DA CONTA", 24, 219, 264, 53, account.SignOut, size: 24);
        signOut.gameObject.SetActive(false);
    }

    void BuildShop()
    {
        var wallet = ui.Panel(safe, "Carteira", -246, 69, 224, 34, RightTop);
        cash = ui.Label(wallet, "", 11, 0, 201, 34, 22, Gold, TextAnchor.MiddleRight, strong: true);

        var shop = ui.Panel(safe, "Loja", -363, 120, 340, 354, RightTop);
        ui.Label(shop, "LOJA", 18, 8, 285, 35, 31, strong: true);
        ui.Label(shop, "Personalize seu operador e arsenal", 19, 40, 287, 31, 18, Muted);
        CategoryRow(shop, "SKINS", "Visuais de operador", 77, skins, OZLobbyInventory.Category.Outfit);
        CategoryRow(shop, "ARMAS", "Acabamentos de rifle", 166, weapons, OZLobbyInventory.Category.Rifle);
        CategoryRow(shop, "PACOTES", "Kits cosméticos", 255, kits, OZLobbyInventory.Category.Kit);
    }

    void CategoryRow(RectTransform shop, string title, string caption, float y, Sprite art, OZLobbyInventory.Category category)
    {
        var card = ui.Button(shop, "", 11, y, 318, 81, () => OpenStore(category));
        Picture(Place(card.transform, title + " • Arte", 5, 5, 140, 71), art, Color.white);
        ui.Label(card.transform, title, 155, 12, 134, 31, 26, strong: true);
        ui.Label(card.transform, caption, 156, 40, 135, 30, 17, Muted);
        ui.Label(card.transform, "›", 288, 0, 23, 81, 39, Gold, TextAnchor.MiddleCenter);
    }

    void Update()
    {
        UpdateSafeArea();
        bool shouldShow = !game.Playing;
        if (renderingLobby != shouldShow)
        {
            renderingLobby = shouldShow;
            portraitPointer = int.MinValue;
            canvas.gameObject.SetActive(shouldShow);
            if (stage) stage.gameObject.SetActive(shouldShow);
            if (game.view) game.view.enabled = !shouldShow || !stage;
        }
        if (!shouldShow) return;

        if (!loadedOutfit && game.Player)
        {
            loadedOutfit = true;
            ApplyAppearance();
        }
        if (play.interactable != (bool)game.Player) play.interactable = game.Player;

        if (!string.IsNullOrEmpty(game.Result) && game.Result != shownResult)
        {
            shownResult = game.Result;
            if (game.Player) OpenResult();
            else startupError = game.Result;
        }

        bool connected = !string.IsNullOrEmpty(account.Token);
        if (connected && !savedSession)
        {
            savedSession = true;
            if (remember) RememberName();
        }
        if (!connected) savedSession = false;
        user.gameObject.SetActive(!connected);
        password.gameObject.SetActive(!connected);
        login.gameObject.SetActive(!connected);
        register.gameObject.SetActive(!connected);
        signOut.gameObject.SetActive(connected);
        login.interactable = register.interactable = !account.AuthPending;
        SetText(accountHeading, connected ? account.Name : "LOGIN / CONTA");
        SetText(userTitle, connected ? "CONECTADO" : "CONVIDADO");
        SetText(status, startupError ?? account.Status);
        int balance = inventory.Balance(game.XP);
        if (balance != shownBalance)
        {
            shownBalance = balance;
            cash.text = balance + "  CRÉDITOS";
        }
        if (ranking) SetText(ranking, account.Ranking);
    }

    static void SetText(Text label, string value)
    {
        if (label.text != value) label.text = value;
    }

    void UpdateSafeArea()
    {
        var resolution = new Vector2Int(Screen.width, Screen.height);
        var area = Screen.safeArea;
        if (lastSafe == area && lastScreen == resolution) return;
        lastSafe = area;
        lastScreen = resolution;
        if (resolution.x <= 0 || resolution.y <= 0) return;
        var size = new Vector2(resolution.x, resolution.y);
        safe.anchorMin = Vector2.Scale(area.min, new Vector2(1 / size.x, 1 / size.y));
        safe.anchorMax = Vector2.Scale(area.max, new Vector2(1 / size.x, 1 / size.y));
        safe.offsetMin = safe.offsetMax = Vector2.zero;

        // Fit the complete layout into the usable area if a cutout reduces it below the design size.
        float factor = Mathf.Min(area.width / 1280f, area.height / 720f);
        var scaler = canvas.GetComponent<CanvasScaler>();
        if (factor > 0)
            scaler.referenceResolution = size / factor;
    }

    void Authenticate(bool signingUp)
    {
        if (!account.Auth(server, user.text, password.text, signingUp)) return;
        password.text = "";
        SetPasswordVisible(false);
    }

    void SetPasswordVisible(bool visible)
    {
        revealPassword = visible;
        password.contentType = visible ? InputField.ContentType.Standard : InputField.ContentType.Password;
        password.ForceLabelUpdate();
        passwordVisibility.text = visible ? "OCULTAR" : "VER";
    }

    void ToggleRemember()
    {
        remember = !remember;
        if (!remember)
        {
            PlayerPrefs.DeleteKey("OZ_LOGIN_NAME");
            PlayerPrefs.Save();
        }
        else if (!string.IsNullOrEmpty(account.Name))
            RememberName();
        UpdateRememberLabel();
    }

    void RememberName()
    {
        PlayerPrefs.SetString("OZ_LOGIN_NAME", account.Name);
        PlayerPrefs.Save();
    }

    void UpdateRememberLabel() => rememberedLabel.text = (remember ? "[X]   " : "[  ]   ") + "Lembrar usuário";

    void ApplyAppearance()
    {
        if (stage) stage.Apply(inventory);
        if (!game.Player) return;
        var grip = Array.Find(game.Player.GetComponentsInChildren<Transform>(true), t => t.name == "WeaponGrip");
        inventory.Apply(game.Player.gameObject, grip, false);
    }

    void Deploy()
    {
        if (!game.Player || game.Playing) return;
        if (EventSystem.current) EventSystem.current.SetSelectedGameObject(null);
        game.Begin(mode);
        if (game.Playing) CloseDialog();
    }

    void CloseDialog()
    {
        if (!dialog) return;
        dialog.gameObject.SetActive(false);
        Destroy(dialog.gameObject);
        dialog = null;
        ranking = null;
    }

    RectTransform OpenDialog(string title, string caption, bool closeable = true)
    {
        CloseDialog();
        portraitPointer = int.MinValue;
        dialog = Layer(safe, title);
        Picture(dialog, null, new Color(.015f, .019f, .025f, .68f), hit: true);
        var shell = ui.Panel(dialog, title + " • Painel", -260, -248, 520, 496, new Vector2(.5f, .5f));
        ui.Label(shell, title, 25, 18, 415, 48, 37, Gold, slanted: true);
        ui.Label(shell, caption, 26, 69, 468, 48, 19, Muted);
        if (closeable)
            ui.Button(shell, "×", 454, 17, 46, 46, CloseDialog, size: 30);
        return shell;
    }

    void OpenModes()
    {
        var panel = OpenDialog("MODOS DE JOGO", "Escolha como quer entrar na Costa de Ferro.");
        for (int index = 0; index < modes.Length; index++)
        {
            var selected = (OZGame.Mode)index;
            ui.Button(panel, $"{modes[index]}  ·  {modeDetails[index]}", 25, 128 + index * 77, 470, 65, () =>
            {
                mode = selected;
                currentMode.text = modes[(int)mode];
                modeDetail.text = modeDetails[(int)mode];
                CloseDialog();
            }, mode == selected);
        }
    }

    void OpenArsenal()
    {
        var panel = OpenDialog("ARSENAL", "Escolha a arma inicial. Os atributos vêm do sistema de combate.");
        for (int index = 0; index < game.WeaponCount; index++)
        {
            int slot = index;
            string info = $"{game.GetWeaponName(slot)}  ·  {game.GetWeaponCapacity(slot)} cartuchos  ·  {game.GetWeaponDamage(slot)} dano";
            ui.Button(panel, info, 25, 123 + index * 68, 470, 55, () =>
            {
                game.SetWeapon(slot);
                OpenArsenal();
            }, game.Weapon == slot, 22);
        }
        ui.Button(panel, "ACABAMENTOS", 281, 405, 214, 52, () => OpenStore(OZLobbyInventory.Category.Rifle));
    }

    void OpenOperator()
    {
        var panel = OpenDialog("OPERADOR", "Vigia • Os uniformes alteram a aparência.");
        string outfit = inventory.Outfit switch { 1 => "Sentinela", 2 => "Anoitecer", _ => "Padrão" };
        ui.Label(panel, "Uniforme equipado", 25, 127, 470, 35, 23, Muted);
        ui.Label(panel, outfit, 25, 166, 470, 82, 54, Gold, slanted: true);
        ui.Button(panel, "USAR UNIFORME PADRÃO", 25, 302, 470, 53, () =>
        {
            inventory.UseStandard(OZLobbyInventory.Category.Outfit);
            ApplyAppearance();
            CloseDialog();
        });
        ui.Button(panel, "VER SKINS", 25, 368, 470, 58, () => OpenStore(OZLobbyInventory.Category.Outfit), true);
    }

    void OpenStore(OZLobbyInventory.Category category)
    {
        string title = category switch
        {
            OZLobbyInventory.Category.Outfit => "LOJA / SKINS",
            OZLobbyInventory.Category.Rifle => "LOJA / ARMAS",
            _ => "LOJA / PACOTES"
        };
        var panel = OpenDialog(title, $"{inventory.Balance(game.XP)} créditos · 1 crédito por 5 XP ganhos em partidas.");
        var message = ui.Label(panel, "", 25, 336, 468, 42, 20, Gold);
        float y = 120;
        foreach (var item in OZLobbyInventory.Items)
        {
            if (item.Category != category) continue;
            bool owned = inventory.Owns(item);
            ui.Label(panel, item.Name, 25, y, 285, 32, 27, strong: true);
            ui.Label(panel, item.Description, 26, y + 32, 288, 55, 19, Muted);
            string action = owned
                ? inventory.IsEquipped(item) ? "EQUIPADO" : "EQUIPAR"
                : $"{inventory.Cost(item)} CRÉDITOS";
            var button = ui.Button(panel, action, 325, y + 12, 170, 49, () =>
            {
                if (!inventory.Equip(item, game.XP))
                {
                    message.text = "Créditos insuficientes. Jogue para ganhar XP.";
                    return;
                }
                ApplyAppearance();
                OpenStore(category);
            }, !owned, 20);
            button.interactable = !owned || !inventory.IsEquipped(item);
            y += 105;
        }
        if (category != OZLobbyInventory.Category.Kit)
            ui.Button(panel, "USAR VISUAL PADRÃO", 25, 399, 470, 54, () =>
            {
                inventory.UseStandard(category);
                ApplyAppearance();
                OpenStore(category);
            });
    }

    void OpenCareer()
    {
        var panel = OpenDialog("CARREIRA", "O nível, o XP e a coleção são guardados neste aparelho.");
        ui.Label(panel, $"NÍVEL {1 + game.XP / 500}", 25, 118, 285, 52, 37, Gold, slanted: true);
        ui.Label(panel, $"{game.XP} XP", 325, 118, 170, 52, 30, White, TextAnchor.MiddleRight, strong: true);
        ui.Button(panel, "CONSULTAR RANKING DO SERVIDOR", 25, 196, 470, 54, () => account.Rank(server));
        ranking = ui.Label(panel, account.Ranking, 25, 263, 470, 173, 22, Muted, TextAnchor.UpperLeft);
    }

    void OpenSettings()
    {
        var panel = OpenDialog("AJUSTES", "Perfis gráficos, limite de quadros e sensibilidade.");
        var qualityNames = new[] { "LEVE", "MÉDIA", "ALTA", "ULTRA" };
        var quality = new Button[qualityNames.Length];
        int selected = PlayerPrefs.GetInt("OZ_QUALITY", 0);
        for (int index = 0; index < quality.Length; index++)
        {
            int level = index;
            quality[index] = ui.Button(panel, qualityNames[index], 25 + 119 * index, 120, 112, 49, () =>
            {
                if (game.Player) game.SetQuality(level);
                else PlayerPrefs.SetInt("OZ_QUALITY", level);
                for (int i = 0; i < quality.Length; i++) quality[i].interactable = i != level;
                PlayerPrefs.Save();
            }, size: 20);
            quality[index].interactable = index != selected;
        }
        int fps = Application.targetFrameRate;
        var rates = game.Supports90Fps ? new[] { 30, 60, 90 } : new[] { 30, 60 };
        var rateButtons = new Button[rates.Length];
        float width = 480f / rates.Length;
        for (int i = 0; i < rates.Length; i++)
        {
            int rate = rates[i];
            rateButtons[i] = ui.Button(panel, rate + " FPS", 25 + i * width, 181, width - 10, 48, () =>
            {
                game.SetFrameRate(rate);
                for (int j = 0; j < rates.Length; j++)
                    rateButtons[j].interactable = rates[j] != Application.targetFrameRate;
                PlayerPrefs.Save();
            });
            rateButtons[i].interactable = rate != fps;
        }

        var sensitivity = ui.Label(panel, "", 25, 240, 470, 35, 22, Muted);
        void AdjustSensitivity(float delta)
        {
            game.Sensitivity = Mathf.Clamp(Mathf.Round((game.Sensitivity + delta) * 10) / 10f, .2f, 3f);
            sensitivity.text = $"Sensibilidade   {game.Sensitivity:0.0}";
            PlayerPrefs.SetFloat("OZ_SENS", game.Sensitivity);
            PlayerPrefs.Save();
        }
        sensitivity.text = $"Sensibilidade   {game.Sensitivity:0.0}";
        ui.Button(panel, "−", 311, 236, 86, 45, () => AdjustSensitivity(-.1f));
        ui.Button(panel, "+", 409, 236, 86, 45, () => AdjustSensitivity(.1f));

        var ads = ui.Label(panel, "", 25, 281, 282, 35, 22, Muted);
        void AdjustAds(float delta)
        {
            game.AdsSensitivity = Mathf.Clamp(Mathf.Round((game.AdsSensitivity + delta) * 10) / 10f, .2f, 3f);
            ads.text = $"Sensibilidade ADS   {game.AdsSensitivity:0.0}";
            PlayerPrefs.SetFloat("OZ_ADS_SENS", game.AdsSensitivity);
            PlayerPrefs.Save();
        }
        ads.text = $"Sensibilidade ADS   {game.AdsSensitivity:0.0}";
        ui.Button(panel, "−", 311, 281, 86, 40, () => AdjustAds(-.1f));
        ui.Button(panel, "+", 409, 281, 86, 40, () => AdjustAds(.1f));

        var endpoint = ui.Input(panel, "Servidor HTTPS", server, 25, 327, 470, 43, InputField.ContentType.Standard, 220);
        var notice = ui.Label(panel, "", 25, 372, 470, 36, 19, Gold);
        ui.Button(panel, "SALVAR SERVIDOR", 25, 410, 470, 53, () =>
        {
            string value = endpoint.text.Trim().TrimEnd('/');
            if (value.Length > 0 && !OZAccount.ValidServer(value))
            {
                notice.text = "Use um endereço HTTPS completo.";
                return;
            }
            if (value != server) account.SignOut();
            server = value;
            PlayerPrefs.SetString("OZ_SERVER", server);
            PlayerPrefs.Save();
            notice.text = "Configuração guardada.";
        });
    }

    void OpenNotes()
    {
        var panel = OpenDialog("ATUALIZAÇÃO V0.7", "Costa de Ferro • Unity");
        ui.Label(panel, "Rotação 360° e enquadramento 3D.\nSensibilidade ADS e troca de ombro.\nVersão em desenvolvimento; testes pendentes.",
            25, 128, 470, 170, 24, White, TextAnchor.UpperLeft);
        ui.Button(panel, "CRÉDITOS E LICENÇAS", 25, 369, 470, 53, OpenCredits);
    }

    void OpenCredits()
    {
        var panel = OpenDialog("CRÉDITOS", "Deslize para ler as atribuições e a licença da fonte.");
        var viewport = Place(panel, "Área de leitura", 25, 122, 470, 344);
        Picture(viewport, null, Invisible, hit: true);
        viewport.gameObject.AddComponent<RectMask2D>();
        var content = Place(viewport, "Licenças", 0, 0, 455, 344);
        var text = ui.Label(content, credits.text + "\n\n" + fontLicense.text, 0, 0, 449, 344, 20,
            Muted, TextAnchor.UpperLeft);
        text.verticalOverflow = VerticalWrapMode.Overflow;
        float height = Mathf.Max(344, text.preferredHeight + 18);
        content.sizeDelta = new Vector2(455, height);
        text.rectTransform.sizeDelta = new Vector2(449, height);
        var scroller = viewport.gameObject.AddComponent<ScrollRect>();
        scroller.viewport = viewport;
        scroller.content = content;
        scroller.horizontal = false;
        scroller.movementType = ScrollRect.MovementType.Clamped;
        scroller.verticalNormalizedPosition = 1;
    }

    void OpenResult()
    {
        var panel = OpenDialog(game.Result, modes[(int)game.mode], false);
        ui.Label(panel, $"{game.Kills} eliminações  ·  {game.XP} XP total", 25, 134, 470, 48, 27);
        ui.Button(panel, "VOLTAR AO LOBBY", 25, 245, 470, 66, game.Back, true, 28);
    }
}
