// File: _Scripts/MovementStrategies/ElephantStrategy.cs

// 象的逻辑和马完全一样，我们可以直接复用HorseStrategy
// 如果未来象有不同，再创建新的类。现在，我们在工厂里直接指向HorseStrategy即可。
// 为了代码清晰，还是创建一个独立的类
public class ElephantStrategy : HorseStrategy
{
    // 目前完全继承马的逻辑，无需任何代码
}