using System;

public interface IViewController
{
    event Action OnBack;
    event Action OnNext;
}
