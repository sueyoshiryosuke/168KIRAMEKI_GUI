﻿namespace MyShogi.Model.Shogi.EngineDefine
{
    /// <summary>
    /// 思考エンジン側がサポートしているUSI拡張機能について表明するために用いる。
    /// 
    /// それぞれの詳しい意味、経緯については"docs/USI2.0.md"を参照のこと。
    /// </summary>
    public enum ExtendedProtocol
    {
        /// <summary>
        /// "go"コマンドでbbyoyomi , wbyoyomiとして先手と後手の秒読み設定を送ってもらう。
        /// また"go ponder" , "ponderhit" 時にも先後の残り時間がやってくる。
        /// 
        /// ※　まだ実装してない。[2018/07/15]
        /// </summary>
        GoCommandTimeExtention,

        /// <summary>
        /// USIプロトコルでは置換表サイズの設定に"USI_Hash"が使われているが、
        /// 将棋所は、個別に置換表サイズを設定できないため、思考エンジン側が独自のオプションで
        /// 置換表サイズの設定が出来るようにしてあるのが実状である。
        /// (やねうら王は、これに該当する。)
        /// 
        /// この拡張を有効にすると、GUI側は、思考エンジンに対して
        /// "USI_Hash"の代わりに"Hash"を送信する。
        /// </summary>
        // UseHashCommandExtension,
        // →　このオプション、廃止。[2020/03/10]
        // "usi"を送信してエンジン側から返ってきた"USI_Hash","Hash"の、存在するほうを送信すれば解決。
        // 両方返ってきた場合、"USI_Hash"を優先。両方返ってこなかった場合も"USI_Hash"。

        /// <summary>
        /// "EvalShare"の機能を持っているのか。
        /// これを持っていて、EvalShareのオプションがtrueになっているなら、
        /// 同じエンジンを2つ起動する時にHASH用のメモリが半分になる。
        /// </summary>
        HasEvalShareOption,

        /// <summary>
        /// 詰将棋エンジンが "go mate nodes XXX"で探索ノード数の制限に対応しているか。
        /// </summary>
        GoMateNodesExtension,

        /// <summary>
        /// 詰将棋エンジンが "go mate depth XXX"で探索深さの制限に対応しているか。
        /// </summary>
        GoMateDepthExtension,
    }
}
