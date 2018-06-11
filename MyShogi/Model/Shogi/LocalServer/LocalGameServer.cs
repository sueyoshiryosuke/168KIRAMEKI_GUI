﻿using MyShogi.Model.Shogi.Core;
using MyShogi.Model.Shogi.Kifu;

namespace MyShogi.Model.LocalServer
{
    /// <summary>
    /// 対局を管理するクラス
    /// 
    /// 内部に局面(Position)を持つ
    /// 内部に棋譜管理クラス(KifuManager)を持つ
    /// 
    /// 思考エンジンへの参照を持つ
    /// プレイヤー入力へのインターフェースを持つ
    /// 時間を管理している。
    /// 
    /// // TODO : あとでなおす
    /// </summary>
    public class LocalGameServer
    {
        public LocalGameServer()
        {
            kifuManager = new KifuManager();
            position = new Position();
            kifuManager.Bind(position);
        }

        public Position position { get; private set;}

        public KifuManager kifuManager { get; private set; }
    }
}