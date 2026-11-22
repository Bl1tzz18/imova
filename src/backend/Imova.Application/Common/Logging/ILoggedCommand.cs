namespace Imova.Application.Common.Logging;

// A command whose outcome is a business event worth one log line (CommandLoggingBehavior): who did
// what to which record, and why it was refused. Put it on commands that change something someone
// may later ask about ("who approved this listing?", "how many sign-ups today?") — not on the
// frequent ones (marking messages read, recording a view).
public interface ILoggedCommand;
