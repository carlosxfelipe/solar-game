using SolarGame.World;

namespace SolarGame;

/// <summary>Pilha de itens iguais em um slot da hotbar.</summary>
public class ItemStack
{
    public Product Product { get; }
    public int Count { get; internal set; }

    public ItemStack(Product product, int count = 1)
    {
        Product = product;
        Count = count;
    }
}

/// <summary>Hotbar estilo Minecraft: 9 slots, itens iguais empilham até 64.</summary>
public class Inventory
{
    public const int SlotCount = 9;
    public const int MaxStack = 64;

    private readonly ItemStack[] _slots = new ItemStack[SlotCount];

    public int Selected { get; private set; }
    public ItemStack this[int index] => _slots[index];
    public ItemStack SelectedStack => _slots[Selected];

    public int TotalItems
    {
        get
        {
            int total = 0;
            foreach (var s in _slots)
                total += s?.Count ?? 0;
            return total;
        }
    }

    /// <summary>Adiciona um item. Retorna o slot usado, ou -1 se o inventário estiver cheio.</summary>
    public int TryAdd(Product product)
    {
        // Primeiro tenta empilhar em um slot com o mesmo produto
        for (int i = 0; i < SlotCount; i++)
        {
            var s = _slots[i];
            if (s != null && s.Product.Name == product.Name && s.Count < MaxStack)
            {
                s.Count++;
                return i;
            }
        }

        // Depois, o primeiro slot vazio
        for (int i = 0; i < SlotCount; i++)
        {
            if (_slots[i] == null)
            {
                _slots[i] = new ItemStack(product);
                return i;
            }
        }
        return -1;
    }

    /// <summary>Tira uma unidade do slot selecionado. Retorna false se ele estiver vazio.</summary>
    public bool RemoveOneFromSelected()
    {
        var s = _slots[Selected];
        if (s == null)
            return false;
        s.Count--;
        if (s.Count <= 0)
            _slots[Selected] = null;
        return true;
    }

    public void Select(int index) => Selected = ((index % SlotCount) + SlotCount) % SlotCount;

    public void Scroll(int delta) => Select(Selected + delta);
}
