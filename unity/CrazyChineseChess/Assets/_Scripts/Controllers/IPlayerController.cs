// File: _Scripts/Controllers/IPlayerController.cs
// (�����½�һ�� _Scripts/Controllers �ļ��������)

/// <summary>
/// �������ӿڣ������˿���һ�����ӵĻ�����Ϊ��
/// ������������롢AI���߻�������ͬ������ͨ��ʵ�ִ˽ӿ�����GameManager������
/// </summary>
public interface IPlayerController
{
    /// <summary>
    /// ��ʼ����������
    /// </summary>
    /// <param name="color">�ÿ��������������ɫ</param>
    /// <param name="gameManager">��Ϸ�ܹ�����������</param>
    void Initialize(PlayerColor color, GameManager gameManager);
}