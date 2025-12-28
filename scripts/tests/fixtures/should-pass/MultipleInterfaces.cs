// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

// This file should PASS: interfaces don't require the one-class-per-file rule
public interface IFirstInterface
{
    void FirstMethod();
}

public interface ISecondInterface
{
    void SecondMethod();
}

public interface IThirdInterface<T>
{
    T GetValue();
    void SetValue(T value);
}
