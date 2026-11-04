using Godot;

namespace CitUp.Game;

/// <summary>Надписи поверх мира: подсказка по выбору здания, сообщения об отказах и статус сети. Висит на узле CanvasLayer.</summary>
public partial class Hud : CanvasLayer
{
    private const double MessageSeconds = 2.0;

    private Label _hint = null!;
    private Label _message = null!;
    private Label _status = null!;
    private double _messageLeft;

    public override void _Ready()
    {
        var info = GetNode<Label>("InfoLabel");
        _hint = GetNode<Label>("HintLabel");
        _message = GetNode<Label>("MessageLabel");

        // Строка статуса сети создаётся здесь же, в сцену добавлять её не нужно
        _status = new Label();
        AddChild(_status);

        // Четыре строки одна под другой, у каждой крупный шрифт и тёмная обводка для читаемости на зелёном
        Place(info, 8f);
        Place(_hint, 36f);
        Place(_message, 64f);
        Place(_status, 92f);

        _message.Text = "";
        _message.AddThemeColorOverride("font_color", new Color(1f, 0.6f, 0.5f));
        _status.AddThemeColorOverride("font_color", new Color(0.7f, 0.95f, 1f));
    }

    private static void Place(Label label, float y)
    {
        label.Position = new Vector2(10f, y);
        label.AddThemeFontSizeOverride("font_size", 20);
        label.AddThemeColorOverride("font_outline_color", new Color(0f, 0f, 0f, 0.85f));
        label.AddThemeConstantOverride("outline_size", 5);
    }

    /// <summary>Показывает, какое здание выбрано. Выбранное в квадратных скобках.</summary>
    public void ShowSelection(Sim.WorldConfig config, int selectedIndex)
    {
        string text = "Строить: ";
        for (int i = 0; i < config.BuildingTypes.Count; i++)
        {
            string name = config.BuildingTypes[i].Name;
            text += i == selectedIndex ? $"[{i + 1} {name}]  " : $"{i + 1} {name}  ";
        }
        _hint.Text = text + "|  ЛКМ: поставить";
    }

    /// <summary>Показывает сообщение на 2 секунды.</summary>
    public void ShowMessage(string text)
    {
        _message.Text = text;
        _messageLeft = MessageSeconds;
    }

    /// <summary>Постоянная строка про состояние сети (режим, адреса, пинг).</summary>
    public void SetNetStatus(string text) => _status.Text = text;

    public override void _Process(double delta)
    {
        if (_messageLeft <= 0)
            return;

        _messageLeft -= delta;
        if (_messageLeft <= 0)
            _message.Text = "";
    }
}