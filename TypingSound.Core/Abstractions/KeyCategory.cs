namespace TypingSound.Core.Abstractions;

/// <summary>
/// キー押下の分類。<b>キーの具体的な値は保持せず</b>、発音に必要な区別だけを表す
/// (プライバシー原則: 内容は記録/送信しない。発音のために「種別」だけを一時利用する)。
/// 必要になった時点で値を追加してよい(拡張は非破壊)。
/// </summary>
public enum KeyCategory
{
    /// <summary>一般キー(文字・記号など、特に区別しないもの)。</summary>
    Other = 0,

    /// <summary>Enter(改行/復帰)。タイプライターのキャリッジリターンに相当。</summary>
    Enter = 1,
}
