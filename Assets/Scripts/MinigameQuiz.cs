
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class QuizManager : MonoBehaviour
{
    [Header("Elementos da Interface")]
    [SerializeField] private TMP_Text pergunta;
    [SerializeField] private TMP_Text timer;

    [SerializeField] private Button opcao1;
    [SerializeField] private Button opcao2;
    [SerializeField] private Button opcao3;
    [SerializeField] private Button opcao4;

    [Header("Configuração do Quiz")]
    [SerializeField] private List<PerguntaQuiz> perguntas;

    private int perguntaAtual = 0;
    private int pontuacao = 0;

    private float tempoRestante = 30f;
    private bool perguntaRespondida = false;

    private Button[] botoes;

    [System.Serializable]
    public class PerguntaQuiz
    {
        public string enunciado;
        public string[] alternativas = new string[4];

        [Range(0, 3)]
        public int respostaCorreta;
    }

    private void Start()
    {
        perguntas = new List<PerguntaQuiz>
        {
            new PerguntaQuiz
            {
                enunciado = "Qual estrutura de dados segue o principio LIFO?",
                alternativas = new string[]
                {
                    "Fila (Queue)",
                    "Pilha (Stack)",
                    "Lista (List)",
                    "Arvore (Tree)"
                },
                respostaCorreta = 1
            },

            new PerguntaQuiz
            {
                enunciado = "Qual componente executa instrucoes de um programa?",
                alternativas = new string[]
                {
                    "Memoria RAM",
                    "Disco rigido",
                    "Processador (CPU)",
                    "Placa de video"
                },
                respostaCorreta = 2
            },

            new PerguntaQuiz
            {
                enunciado = "Qual protocolo atribui enderecos IP automaticamente?",
                alternativas = new string[]
                {
                    "DNS",
                    "HTTP",
                    "FTP",
                    "DHCP"
                },
                respostaCorreta = 3
            }
        };

        botoes = new Button[]
        {
            opcao1,
            opcao2,
            opcao3,
            opcao4
        };

        for (int i = 0; i < botoes.Length; i++)
        {
            int indice = i;

            botoes[i].onClick.AddListener(
                () => VerificarResposta(indice)
            );
        }

        ExibirPergunta();
    }

    private void Update()
    {
        if (perguntaRespondida || perguntaAtual >= perguntas.Count)
            return;

        tempoRestante -= Time.deltaTime;

        timer.text = Mathf.CeilToInt(tempoRestante).ToString();

        if (tempoRestante <= 0)
        {
            tempoRestante = 0;
            perguntaRespondida = true;

            Debug.Log("Tempo esgotado!");

            ProximaPergunta();
        }
    }

    private void ExibirPergunta()
    {
        if (perguntaAtual >= perguntas.Count)
        {
            FinalizarQuiz();
            return;
        }

        perguntaRespondida = false;
        tempoRestante = 30f;

        timer.text = "30";

        PerguntaQuiz atual = perguntas[perguntaAtual];

        pergunta.text =
            $"Pergunta {perguntaAtual + 1}/{perguntas.Count}\n\n" +
            atual.enunciado;

        for (int i = 0; i < botoes.Length; i++)
        {
            TMP_Text texto =
                botoes[i].GetComponentInChildren<TMP_Text>();

            texto.text = atual.alternativas[i];

            botoes[i].interactable = true;
        }
    }

    private void VerificarResposta(int alternativa)
    {
        if (perguntaRespondida)
            return;

        perguntaRespondida = true;

        PerguntaQuiz atual = perguntas[perguntaAtual];

        if (alternativa == atual.respostaCorreta)
        {
            pontuacao++;
            Debug.Log("Resposta correta!");
        }
        else
        {
            Debug.Log("Resposta incorreta!");
        }

        ProximaPergunta();
    }

    private void ProximaPergunta()
    {
        foreach (Button botao in botoes)
        {
            botao.interactable = false;
        }

        perguntaAtual++;

        ExibirPergunta();
    }

    private void FinalizarQuiz()
    {
        perguntaRespondida = true;

        pergunta.text =
            "Quiz finalizado!\n\n" +
            $"Pontuacao: {pontuacao}/{perguntas.Count}";

        timer.text = "";

        foreach (Button botao in botoes)
        {
            botao.gameObject.SetActive(false);
        }

        Debug.Log($"Pontuacao final: {pontuacao}");
    }
}
