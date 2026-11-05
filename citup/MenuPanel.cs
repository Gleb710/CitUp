using System;
using Godot;

namespace CitUp.Game;

/// <summary>Стартовое меню: одиночная игра, создание игры (хост), подключение. Весь интерфейс собирается в коде.</summary>
public partial class MenuPanel : CanvasLayer
{
    public event Action? SoloPressed;
    public event Action<string, int>? HostPressed;             // имя, порт
    public event Action<string, string, int>? JoinPressed;     // имя, адрес, порт

    private LineEdit _name = null!;
    private LineEdit _address = null!;
    private LineEdit _port = null!;
    private Label _status = null!;

    public override void _Ready()
    {
        Layer = 10;   // поверх остального интерфейса

        // Затемнение на весь экран; заодно не пускает клики к игре под меню
        var dim = new ColorRect { Color = new Color(0f, 0f, 0f, 0.55f) };
        dim.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        AddChild(dim);

        // CenterContainer ставит содержимое по центру экрана
        var center = new CenterContainer();
        center.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        AddChild(center);

        var panel = new PanelContainer();
        center.AddChild(panel);

        var margin = new MarginContainer();
        foreach (var side in new[] { "margin_left", "margin_top", "margin_right", "margin_bottom" })
            margin.AddThemeConstantOverride(side, 20);
        panel.AddChild(margin);

        // VBoxContainer раскладывает элементы столбиком
        var box = new VBoxContainer { CustomMinimumSize = new Vector2(380f, 0f) };
        box.AddThemeConstantOverride("separation", 8);
        margin.AddChild(box);

        box.AddChild(new Label { Text = "CitUp", HorizontalAlignment = HorizontalAlignment.Center });

        // Случайный хвост в имени нужен, чтобы два окна на одном компьютере не оказались «одним игроком»
        _name = AddField(box, "Имя игрока", $"Игрок{Random.Shared.Next(100, 1000)}");

        var solo = new Button { Text = "Играть одному" };
        solo.Pressed += () => SoloPressed?.Invoke();
        box.AddChild(solo);

        box.AddChild(new HSeparator());

        _address = AddField(box, "Адрес хоста (для подключения)", "127.0.0.1");
        _port = AddField(box, "Порт", NetManager.DefaultPort.ToString());

        var host = new Button { Text = "Создать игру (хост)" };
        host.Pressed += OnHostClicked;
        box.AddChild(host);

        var join = new Button { Text = "Подключиться" };
        join.Pressed += OnJoinClicked;
        box.AddChild(join);

        _status = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        box.AddChild(_status);
    }

    public void SetStatus(string text) => _status.Text = text;

    public void CloseMenu() => Visible = false;

    /// <summary>Снова показывает меню (например, после выхода из партии). Текст статуса можно передать сразу.</summary>
    public void OpenMenu(string status = "")
    {
        SetStatus(status);
        Visible = true;
    }
    
    private static LineEdit AddField(Container parent, string caption, string text)
    {
        parent.AddChild(new Label { Text = caption });
        var edit = new LineEdit { Text = text };
        parent.AddChild(edit);
        return edit;
    }

    private string PlayerName => string.IsNullOrWhiteSpace(_name.Text) ? "Игрок" : _name.Text.Trim();

    private bool TryReadPort(out int port)
    {
        if (int.TryParse(_port.Text, out port) && port is >= 1024 and <= 65535)
            return true;

        SetStatus("Порт должен быть числом от 1024 до 65535.");
        return false;
    }

    private void OnHostClicked()
    {
        if (TryReadPort(out int port))
            HostPressed?.Invoke(PlayerName, port);
    }

    private void OnJoinClicked()
    {
        string address = _address.Text.Trim();
        if (address.Length == 0)
        {
            SetStatus("Введите адрес хоста.");
            return;
        }

        if (TryReadPort(out int port))
            JoinPressed?.Invoke(PlayerName, address, port);
    }
}