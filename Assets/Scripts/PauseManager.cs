using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class PauseManager : MonoBehaviour
{
    [Header("Painéis de UI")]
    public GameObject pauseMenuUI;
    public GameObject opcoesPanelUI;
    public GameObject primeiroBotaoSelecionado;

    [Header("Configurações de Áudio")]
    public Slider sliderVolume;

    [Header("Sprites - Jogador (Alto Contraste)")]
    public SpriteRenderer jogadorSpriteRenderer;
    public Sprite spriteJogadorNormal;
    public Sprite spriteJogadorDaltonico;

    [Header("Sprites - Raios (Alto Contraste)")]
    public Sprite spriteRaioNormal;
    public Sprite spriteRaioDaltonico;

    public static bool isPaused = false;
    public static bool modoDaltonicoAtivo = false;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
            {
                if (opcoesPanelUI != null && opcoesPanelUI.activeSelf)
                {
                    FecharOpcoes();
                }
                else
                {
                    Resume();
                }
            }
            else
            {
                Pause();
            }
        }
    }

    public void Resume()
    {
        if (opcoesPanelUI != null) opcoesPanelUI.SetActive(false);
        pauseMenuUI.SetActive(false);
        Time.timeScale = 1f;
        isPaused = false;
    }

    public void Pause()
    {
        pauseMenuUI.SetActive(true);
        if (opcoesPanelUI != null) opcoesPanelUI.SetActive(false);
        Time.timeScale = 0f;
        isPaused = true;

        if (primeiroBotaoSelecionado != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(primeiroBotaoSelecionado);
        }
    }

    public void AbrirOpcoes()
    {
        pauseMenuUI.SetActive(false);
        opcoesPanelUI.SetActive(true);
    }

    public void FecharOpcoes()
    {
        opcoesPanelUI.SetActive(false);
        pauseMenuUI.SetActive(true);
    }

    public void AlterarVolume(float valor)
    {
        AudioListener.volume = valor;
    }

    public void VoltarAoMenuPrincipal(string nomeCenaMenu)
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(nomeCenaMenu);
    }

    // Função conectada ao On Value Changed do Dropdown de Acessibilidade
    public void AlterarModoDaltonico(int indiceOpcao)
    {
        modoDaltonicoAtivo = (indiceOpcao == 1);

        // 1. Troca o Sprite do Jogador
        if (jogadorSpriteRenderer != null)
        {
            jogadorSpriteRenderer.sprite = modoDaltonicoAtivo ? spriteJogadorDaltonico : spriteJogadorNormal;
        }

        // 2. Troca os Sprites de todos os raios que já estão caindo na cena
        AtualizarRaiosExistentes();
    }

    private void AtualizarRaiosExistentes()
    {
        NotificacaoItem[] notificacoesAtivas = FindObjectsByType<NotificacaoItem>(FindObjectsSortMode.None);
        foreach (NotificacaoItem item in notificacoesAtivas)
        {
            item.AtualizarSprite();
        }
    }
}