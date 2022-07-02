﻿using System.Collections.Generic;
using RogueElements;
using RogueEssence.Content;
using RogueEssence.Dungeon;
using RogueEssence.Data;
using System;

namespace RogueEssence.Menu
{
    public class VersionResultsMenu : SideScrollMenu
    {
        public const int MAX_LINES = 13;

        public MenuText Title;
        public MenuDivider Div;
        public MenuText[][] Versions;

        public GameProgress Ending;
        public int Page;

        public VersionResultsMenu(GameProgress ending, int page)
        {
            Bounds = Rect.FromPoints(new Loc(GraphicsManager.ScreenWidth / 2 - 140, 16), new Loc(GraphicsManager.ScreenWidth / 2 + 140, 224));
            Ending = ending;
            Page = page;

            List<ModVersion> versionData = ending.GetModVersion();
            Title = new MenuText(Text.FormatKey("MENU_VERSION_TITLE", Page + 1, MathUtils.DivUp(versionData.Count, MAX_LINES)), new Loc(Bounds.Width / 2, GraphicsManager.MenuBG.TileHeight), DirH.None);

            Div = new MenuDivider(new Loc(GraphicsManager.MenuBG.TileWidth, GraphicsManager.MenuBG.TileHeight + LINE_HEIGHT), Bounds.Width - GraphicsManager.MenuBG.TileWidth * 2);

            int displayTotal = Math.Min(MAX_LINES, versionData.Count - Page * MAX_LINES);
            Versions = new MenuText[displayTotal][];
            for (int ii = 0; ii < displayTotal; ii++)
            {
                Versions[ii] = new MenuText[2];
                Versions[ii][0] = new MenuText(versionData[ii].Name, new Loc(GraphicsManager.MenuBG.TileWidth * 2, GraphicsManager.MenuBG.TileHeight + VERT_SPACE * ii + TitledStripMenu.TITLE_OFFSET));
                Versions[ii][1] = new MenuText(versionData[ii].VersionString, new Loc(Bounds.Width - GraphicsManager.MenuBG.TileWidth * 2, GraphicsManager.MenuBG.TileHeight + VERT_SPACE * ii + TitledStripMenu.TITLE_OFFSET), DirH.Right);
            }

            base.Initialize();
        }

        public override IEnumerable<IMenuElement> GetElements()
        {
            yield return Title;
            yield return Div;

            foreach (MenuText[] arr in Versions)
                foreach(MenuText item in arr)
                    yield return item;
        }

        public override void Update(InputManager input)
        {
            if (input.JustPressed(FrameInput.InputType.Menu) || input.JustPressed(FrameInput.InputType.Confirm)
                || input.JustPressed(FrameInput.InputType.Cancel))
            {
                GameManager.Instance.SE("Menu/Confirm");
                MenuManager.Instance.RemoveMenu();
            }
            else if (IsInputting(input, Dir8.Left))
            {
                GameManager.Instance.SE("Menu/Skip");
                if (Page > 0)
                    MenuManager.Instance.ReplaceMenu(new VersionResultsMenu(Ending, Page - 1));
                else
                {
                    if (Ending.ActiveTeam.Assembly.Count > 0)
                        MenuManager.Instance.ReplaceMenu(new AssemblyResultsMenu(Ending, (Ending.ActiveTeam.Assembly.Count - 1) / 4));
                    else
                        MenuManager.Instance.ReplaceMenu(new PartyResultsMenu(Ending));
                }
            }
            else if (IsInputting(input, Dir8.Right))
            {
                GameManager.Instance.SE("Menu/Skip");
                if (Page < (Ending.GetModVersion().Count - 1) / MAX_LINES)
                    MenuManager.Instance.ReplaceMenu(new VersionResultsMenu(Ending, Page + 1));
                else
                    MenuManager.Instance.ReplaceMenu(new FinalResultsMenu(Ending));
            }

        }
    }
}
