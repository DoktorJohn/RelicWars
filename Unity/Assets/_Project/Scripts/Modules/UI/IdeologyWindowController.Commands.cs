using System;
using Assets.Scripts.Domain.State;
using Assets._Project.Scripts.Domain.Enums;
using Project.Modules.City;
using Project.Modules.WorldPlayer;
using Project.Network.Manager;
using Project.Scripts.Domain.DTOs;
using UnityEngine;
using UnityEngine.UIElements;

namespace Assets._Project.Scripts.Modules.UI
{
    public partial class IdeologyWindowController
    {
        private void ExecuteEnactFocus(IdeologyFocusNameEnum focusName, Button clickedButton)
        {
            clickedButton.SetEnabled(false);
            string token = NetworkManager.Instance.JwtToken;

            double pointCost = ((FocusButtonState)clickedButton.userData).Cost;

            if (WorldPlayerStateManager.Instance != null)
            {
                WorldPlayerStateManager.Instance.DeductResourcesLocally(0, 0, pointCost);
            }

            var requestDto = new IdeologyFocusRequestDTO
            {
                CityId = _currentActiveCityId,
                IdeologyFocusName = focusName
            };

            StartCoroutine(NetworkManager.Instance.IdeologyFocus.EnactIdeologyFocus(requestDto, token, result =>
            {
                if (result != null && result.Success)
                {
                    if (result.EffectResult != null)
                    {
                        StartCoroutine(ShowEffectResultAndRefresh(result.EffectResult.Summary));
                    }
                    else
                    {
                        RequestAndRenderIdeologyData(_requestVersion);
                    }

                    if (Guid.TryParse(NetworkManager.Instance.WorldPlayerId, out Guid wpId) && WorldPlayerStateManager.Instance != null)
                    {
                        WorldPlayerStateManager.Instance.InitiateEconomyRefresh(wpId);
                    }

                    if (CityStateManager.Instance != null && _currentActiveCityId != Guid.Empty)
                    {
                        CityStateManager.Instance.RequestImmediateRefresh(_currentActiveCityId);
                    }
                }
                else
                {
                    if (WorldPlayerStateManager.Instance != null)
                    {
                        WorldPlayerStateManager.Instance.DeductResourcesLocally(0, 0, -pointCost);
                    }

                    Debug.LogError($"[IdeologyWindow] Enact failed: {result?.Message}");
                    clickedButton.SetEnabled(true);
                }
            }));
        }
    }
}
