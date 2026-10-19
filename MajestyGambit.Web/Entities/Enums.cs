namespace MajestyGambit.Web.Entities;

public enum UserStatus { offline, online, in_game }

public enum TimeControl { bullet, blitz, rapid, classical }

public enum RoomStatus { waiting, in_progress, finished, cancelled }

public enum PieceColor { white, black }

public enum GameResult { white_win, black_win, draw }

public enum WinnerColor { white, black }

public enum EndReason
{
    checkmate, resign, timeout,
    stalemate, insufficient_material,
    threefold_repetition, agreement
}
