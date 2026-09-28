using SpeedtestWatcher.Application.Enums;

namespace SpeedtestWatcher.Application.Models;

internal sealed record OutgoingMessage(MessageKind Kind, string Text);
