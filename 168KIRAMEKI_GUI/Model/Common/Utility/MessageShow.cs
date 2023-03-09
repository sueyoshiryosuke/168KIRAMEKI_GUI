﻿using System.Windows.Forms;

namespace MyShogi.Model.Common.Utility
{
    // -- MessageBoxに表示するヘルパークラス

    /// <summary>
    /// TheApp.MessageShow()の引数で使う通知のタイプ。
    /// </summary>
    public enum MessageShowType
    {
        Information,         // 単なる通知
        InformationOkCancel, // 通知でOkとCancelがあるタイプのダイアログ。
        Warning,             // 警告
        WarningOkCancel,     // 警告でOkとCancelがあるタイプのダイアログ
        Error,               // エラー
        ErrorOkCancel,       // エラーでOkとCancelがあるタイプのダイアログ
        Confirmation,        // 確認
        ConfirmationOkCancel,// 確認でOkとCancelがあるタイプのダイアログ
        Exception,           // 例外
    }

    public static class MessageShowTypeExtensions
    {
        /// <summary>
        /// MessageShowType型を綺麗な日本語文字列に変換
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string Pretty(this MessageShowType type)
        {
            switch (type)
            {
                case MessageShowType.Information:
                case MessageShowType.InformationOkCancel:
                    return "通知";
                case MessageShowType.Warning:
                case MessageShowType.WarningOkCancel:
                    return "警告";
                case MessageShowType.Error:
                case MessageShowType.ErrorOkCancel:
                    return "エラー";
                case MessageShowType.Confirmation:
                case MessageShowType.ConfirmationOkCancel:
                    return "確認";
                case MessageShowType.Exception:
                    return "例外";

                default: return "";
            }
        }

        /// <summary>
        /// MessageShowType型を綺麗な日本語文字列に変換
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static MessageBoxIcon ToIcon(this MessageShowType type)
        {
            switch (type)
            {
                case MessageShowType.Information:
                case MessageShowType.InformationOkCancel:
                    return MessageBoxIcon.Asterisk;
                case MessageShowType.Warning:
                case MessageShowType.WarningOkCancel:
                    return MessageBoxIcon.Exclamation;
                case MessageShowType.Error:
                case MessageShowType.ErrorOkCancel:
                case MessageShowType.Exception:
                    return MessageBoxIcon.Hand;
                case MessageShowType.Confirmation:
                case MessageShowType.ConfirmationOkCancel:
                    return MessageBoxIcon.Question; // このIcon、現在、非推奨なのか…。まあいいや。
                default: return MessageBoxIcon.None;
            }
        }

        public static MessageBoxButtons ToButtons(this MessageShowType type)
        {
            switch (type)
            {
                case MessageShowType.Information:
                case MessageShowType.Warning:
                case MessageShowType.Error:
                case MessageShowType.Exception:
                case MessageShowType.Confirmation:
                    return MessageBoxButtons.OK;

                case MessageShowType.InformationOkCancel:
                case MessageShowType.WarningOkCancel:
                case MessageShowType.ErrorOkCancel:
                case MessageShowType.ConfirmationOkCancel:
                    return MessageBoxButtons.OKCancel;

                default:
                    return MessageBoxButtons.OK;
            }
        }
    }
}
