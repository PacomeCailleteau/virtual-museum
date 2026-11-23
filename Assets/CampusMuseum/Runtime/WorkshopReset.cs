using System.Linq;
using UnityEngine;
using VirtualMuseum;
using static CampusMuseum.WorldView;

namespace CampusMuseum
{
    public sealed partial class MuseumApp
    {
        void TickResetKeys()
        {
            var keyboard=UnityEngine.InputSystem.Keyboard.current;
            if(keyboard==null)return;
            if(keyboard.f5Key.wasPressedThisFrame)ResetWorkshop(false);
            if(keyboard.f6Key.wasPressedThisFrame)ResetWorkshop(true);
        }

        void InitializeBuzzers()
        {
            BindBuzzer(workshop.resetBuzzer,false);
            BindBuzzer(workshop.emptyBuzzer,true);
        }

        void BindBuzzer(Transform anchor,bool empty)
        {
            if(!anchor)return;
            var cap=anchor.Find("Capuchon");
            var buzzer=cap.gameObject.GetComponent<PunchBuzzer>()??cap.gameObject.AddComponent<PunchBuzzer>();
            buzzer.Initialize(player,()=>ResetWorkshop(empty));
        }

        bool ResetWorkshop(bool empty)
        {
            if(!WorkshopReady() || visiting)return false;
            if(empty)Api.Replace(new Museum(),new VisitorSession());
            else Api.ResetToExample();
            visitHistory.Clear();
            selectedRoom=Api.Data.rooms.FirstOrDefault()?.id;
            selectedTour=Api.Data.tours.FirstOrDefault()?.id;
            selectedArt=linkFirstRoom=lastAddedArtwork=null;
            linking=drawing=confirmDelete=guided=false;corner=null;activeField=TextField.None;editTour=false;
            tourPage=artPage=stopPage=selectedStep=0;boardNotice=null;boardSection=BoardSection.Tours;
            Tell(empty?"Maquette vide. Trace la première salle.":"Musée initial restauré : 3 salles, 6 œuvres et 2 parcours.");
            return true;
        }
    }
}
