using m3u8_winui.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace m3u8_winui
{
    public interface IDataService
    {
        IList<HeaderView> GetItems();
        HeaderView GetItem(int id);
        int AddItem(HeaderView item);
        void UpdateItem(HeaderView item);
        HeaderView GetMedium(string name);
        IList<HeaderView> GetMediums();
        IList<HeaderView> GetMediums(HeaderView itemType);
    }
}
