﻿using MyShogi.Model.Shogi.Core;
using System.Diagnostics;

namespace MyShogi.Model.Resource.Sounds
{
    /// <summary>
    /// サウンドの再生のための定数
    /// </summary>
    public enum SoundEnum
    {
        // -- 駒の音声

        // 駒の移動させた時の「カチ」っという音
        KOMA_S1 ,

        // 駒の移動先の升の下に別の駒があった時に駒のぶつかる音が軽く聞こえる音
        // B1のBは、「ぶつかる」のBか？
        KOMA_B1, 

        // 王手の時と駒を捕獲した時に衝撃音を鳴らす場合、このサウンド
        KOMA_C1,

        // -- 以下、読み上げの音声

        // SQ_11 ～ SQ_99
        SQ_11, // 11の升の音声
        // (中略)
        SQ_99 = SQ_11 + 80,
        // 各駒
        PiecePAWN,
        // (中略)
        PieceLast = PiecePAWN + 16,

        // 「よろしくお願いします」
        Start ,
        // 「ありがとうございました。またお願いします。」
        End ,

        // 「先手」「後手」「上手(うわて)」「下手(したて)」
        Sente,
        Gote ,
        Uwate , 
        Shitate ,

        // 「成り」「不成」
        Naru ,
        Narazu,

        // 「同じく」
        Onajiku,

        // 「右」「左」「直」「引く」「打つ」「寄る」「上がる」「行く」
        Migi , Hidari , Sugu , Hiku , Utsu , Yoru , Agaru , Yuku ,

        // 「持将棋」「千日手」「詰み」「時間切れ」
        Jisyougi , Sennichite , Tsumi, Jikangire ,

        // -- 秒読み

        BYOYOMI_10BYO,
        BYOYOMI_20BYO,
        BYOYOMI_30BYO,
        BYOYOMI_40BYO,
        BYOYOMI_50BYO,

        BYOYOMI_1, BYOYOMI_2, BYOYOMI_3, BYOYOMI_4, BYOYOMI_5,
        BYOYOMI_6, BYOYOMI_7, BYOYOMI_8, BYOYOMI_9,

        // -- 種類ごとの開始～終了の定数

        KOMA_START = KOMA_S1,
        KOMA_END = KOMA_C1,

        READ_OUT_START = SQ_11, // 読み上げの音声の開始
        READ_OUT_END = Jikangire,

        BYOYOMI_START = BYOYOMI_10BYO, // 秒読み音声の開始
        BYOYOMI_END = BYOYOMI_9,
    }

    /// <summary>
    /// サウンドに対応するファイル名を取得する
    /// </summary>
    public static class SoundHelper
    {
        /// <summary>
        /// 駒音であるか。
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public static bool IsKoma(this SoundEnum e)
        {
            return SoundEnum.KOMA_START <= e && e <= SoundEnum.KOMA_END;
        }

        /// <summary>
        /// 秒読みの音声であるか。
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public static bool IsByoyomi(this SoundEnum e)
        {
            return SoundEnum.BYOYOMI_START <= e && e <= SoundEnum.BYOYOMI_END;
        }

        /// <summary>
        /// 対応する音声ファイル名を返す。
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public static string FileNameOf(SoundEnum e)
        {
            if (e.IsKoma())
                return KomaFileNameOf(e);

            if (SoundEnum.SQ_11 <= e && e <= SoundEnum.SQ_99)
                return FileNameOf((Square)(e - SoundEnum.SQ_11));

            if (SoundEnum.PiecePAWN <= e && e <= SoundEnum.PieceLast)
                return FileNameOf((Piece)(e - SoundEnum.PiecePAWN + (int)Piece.PAWN));

            if (e.IsByoyomi())
                return ByoyomiFileNameOf(e);
            
            // それ以外なので特別なもののはず..
            return SpecialFileNameOf(e);
        }

        /// <summary>
        /// 駒音に対応するファイル名を返す。
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        private static string KomaFileNameOf(SoundEnum e)
        {
            switch (e)
            {
                case SoundEnum.KOMA_S1: return "koma_s1.wav";
                case SoundEnum.KOMA_B1: return "koma_b1.wav";
                case SoundEnum.KOMA_C1: return "koma_c1.wav";
                default: return "";
            }
        }

        /// <summary>
        /// 升名に対応する音声ファイル名を返す。
        /// </summary>
        /// <param name="sq"></param>
        /// <returns></returns>
        private static string FileNameOf(Square sq)
        {
            Debug.Assert(sq.IsOk());

            return sq.ToNumString() + ".wav";
        }

        /// <summary>
        /// 駒名に対応する音声ファイル名を返す。
        /// </summary>
        /// <param name="pc"></param>
        /// <returns></returns>
        private static string FileNameOf(Piece pc)
        {
            switch (pc)
            {
                case Piece.PAWN:   return "fu_a.wav";
                case Piece.LANCE:  return "kyou_a.wav";
                case Piece.KNIGHT: return "kei_a.wav";
                case Piece.SILVER: return "gin_a.wav";
                case Piece.GOLD:   return "kin_a.wav";
                case Piece.BISHOP: return "kaku_a.wav";
                case Piece.ROOK:   return "hi_a.wav";
                case Piece.KING:   return "gyoku_a.wav";

                case Piece.PRO_PAWN:   return "tokin_a.wav";
                case Piece.PRO_LANCE:  return "narikyou_a.wav";
                case Piece.PRO_KNIGHT: return "narikei_a.wav";
                case Piece.PRO_SILVER: return "narigin_a.wav";
                case Piece.HORSE:      return "uma_a.wav";
                case Piece.DRAGON:     return "ryuu_a.wav";

                default: return "";
            }
        }

        /// <summary>
        /// SoundEnumの秒読みに対応するファイル名を返す
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        private static string ByoyomiFileNameOf(SoundEnum e)
        {
            switch (e)
            {
                case SoundEnum.BYOYOMI_10BYO: return "10byou.wav";
                case SoundEnum.BYOYOMI_20BYO: return "20byou.wav";
                case SoundEnum.BYOYOMI_30BYO: return "30byou.wav";
                case SoundEnum.BYOYOMI_40BYO: return "40byou.wav";
                case SoundEnum.BYOYOMI_50BYO: return "50byou.wav";
                case SoundEnum.BYOYOMI_1: return "01.wav";
                case SoundEnum.BYOYOMI_2: return "02.wav";
                case SoundEnum.BYOYOMI_3: return "03.wav";
                case SoundEnum.BYOYOMI_4: return "04.wav";
                case SoundEnum.BYOYOMI_5: return "05.wav";
                case SoundEnum.BYOYOMI_6: return "06.wav";
                case SoundEnum.BYOYOMI_7: return "07.wav";
                case SoundEnum.BYOYOMI_8: return "08.wav";
                case SoundEnum.BYOYOMI_9: return "09.wav";

                default: return "";
            }
        }

        /// <summary>
        /// SoundEnumの特別なものに対応するファイル名を返す
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        private static string SpecialFileNameOf(SoundEnum e)
        {
            switch (e)
            {
                case SoundEnum.Start:   return "start.wav";
                case SoundEnum.End:     return "win.wav";

                case SoundEnum.Sente:   return "sente.wav";
                case SoundEnum.Gote:    return "gote.wav";
                case SoundEnum.Uwate:   return "uwate.wav";
                case SoundEnum.Shitate: return "shitate.wav";

                case SoundEnum.Naru:    return "naru.wav";
                case SoundEnum.Narazu:  return "narazu.wav";

                case SoundEnum.Onajiku: return "onajiku.wav";

                case SoundEnum.Migi:    return "migi.wav";
                case SoundEnum.Hidari:  return "hidari.wav";
                case SoundEnum.Sugu:    return "sugu.wav";
                case SoundEnum.Hiku:    return "hiku.wav";
                case SoundEnum.Utsu:    return "utsu.wav";
                case SoundEnum.Yoru:    return "yoru.wav";
                case SoundEnum.Agaru:   return "agaru.wav";
                case SoundEnum.Yuku:    return "yuku.wav";

                case SoundEnum.Jisyougi:  return "jisyougi.wav";
                case SoundEnum.Sennichite:return "sennichite.wav";
                case SoundEnum.Tsumi:     return "tsumi.wav";
                case SoundEnum.Jikangire: return "jikangire.wav";

                default: return "";
            }
        }

    }
}
