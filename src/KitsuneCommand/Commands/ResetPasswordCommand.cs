using System;
using System.Collections.Generic;
using Autofac;
using KitsuneCommand.Core;
using KitsuneCommand.Data.Repositories;
using KitsuneCommand.Web.Auth;

namespace KitsuneCommand.Commands
{
    /// <summary>
    /// Console command for resetting a KitsuneCommand web-panel user's password.
    /// Usage:
    ///   kcresetpw &lt;username&gt; &lt;newpassword&gt;
    ///
    /// This is the escape hatch when an admin is locked out of the panel — runs
    /// on the 7D2D game console, telnet, or (once wired) KC's own Console view.
    /// Zero external deps: server admins already have shell or in-game console
    /// access, so no SMTP / email flow to maintain.
    ///
    /// Routine password changes should still happen via the panel's
    /// Settings → Account tab. This command is for the "I can't log in" case.
    /// </summary>
    public class ResetPasswordCommand : ConsoleCmdAbstract
    {
        private const int MinPasswordLength = 8;

        // Admin-only. 7D2D permission levels: 0 = highest (server owner), 1000 = anyone.
        // Anyone with game-console access is trusted enough for this, but gate it to
        // server-owner level by default so a mid-tier mod/admin can't rotate the
        // top-level KC admin's password.
        public override int DefaultPermissionLevel => 0;

        public override string[] getCommands()
        {
            return new[] { "kcresetpw", "kc-reset-password" };
        }

        public override string getDescription()
        {
            return "Reset a KitsuneCommand panel user's password (admin only). Usage: kcresetpw <username> <newpassword>";
        }

        public override string getHelp()
        {
            return
                "Resets a KitsuneCommand panel user's password. Admin only.\n" +
                "\n" +
                "Usage:\n" +
                "  kcresetpw <username> <newpassword>\n" +
                "\n" +
                "Notes:\n" +
                "  - <newpassword> must be at least " + MinPasswordLength + " characters.\n" +
                "  - Prefer the panel's 'Users' / 'Account' tab for routine password changes —\n" +
                "    this command is the escape hatch when you can't log in.\n" +
                "  - The password is hashed before write; the plaintext is not logged.";
        }

        public override void Execute(List<string> _params, CommandSenderInfo _senderInfo)
        {
            var console = SdtdConsole.Instance;

            if (_params == null || _params.Count < 2)
            {
                console.Output("Usage: kcresetpw <username> <newpassword>");
                return;
            }

            var username = _params[0];
            var newPassword = _params[1];

            if (newPassword.Length < MinPasswordLength)
            {
                console.Output($"Password must be at least {MinPasswordLength} characters.");
                return;
            }

            var container = ModLifecycle.Container;
            if (container == null)
            {
                console.Output("KitsuneCommand isn't fully initialized yet. Wait for server boot to complete, then retry.");
                return;
            }

            try
            {
                var userRepo = container.Resolve<IUserAccountRepository>();
                var account = userRepo.GetByUsername(username);
                if (account == null)
                {
                    console.Output($"No KC user named '{username}'. Use the panel's Users tab to create one, or check the spelling.");
                    return;
                }

                var newHash = PasswordHasher.Hash(newPassword);
                userRepo.UpdatePassword(account.Id, newHash);

                // Log sans password so audit trail exists but plaintext doesn't end up in logs.
                Log.Out($"[KitsuneCommand] Password reset for user '{username}' via console by '{_senderInfo.RemoteClientInfo?.playerName ?? "local"}'");
                console.Output($"Password reset for '{username}'. They can now log into the panel with the new password.");
            }
            catch (Exception ex)
            {
                Log.Error($"[KitsuneCommand] kcresetpw failed: {ex}");
                console.Output($"Password reset failed: {ex.Message}");
            }
        }
    }
}
