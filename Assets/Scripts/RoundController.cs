﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Model;
using UnityEngine;
using Random = UnityEngine.Random;

public class RoundController : MonoBehaviour
{
    public MainMenuController MainMenu;
    public bool IsAllowToSameCharacters;
    public int OpponentsCount = 3;
    public int VariantsCount = 3;
    public CharacterObject PlayerCharacter;
    public CharacterObject[] Opponents;

    public Action<ICharacter[]> OnRoundStarted;
    public Action<ICharacter, ICharacter, SentenceObject[]> OnActivePlayerChanged;
    public Action<ICharacter, SentenceObject> OnActivePlayerAnswered;

    private LinkedList<ICharacter> _players;
    private LinkedListNode<ICharacter> _currentTurn;

    public AudioClip GameMusic;
    public AudioClip FinishSound;

    private float _startTime = 0;
    private float ROUND_TIME = 300;
    private bool _isLoose = true;

    public bool IsGameLoose
    {
        get { return _isLoose; }
    }

    public bool FailIfAllPlayersZero = true;

    public float RoundTime { get; private set; }

    public void StartRound()
    {
        _startTime = Time.time;
        _players = CreatePlayers(ChooseOpponents());

        if (OnRoundStarted != null)
        {
            OnRoundStarted(_players.ToArray());
        }
        
        SoundController.Play(GameMusic, true);
        
        Invoke("NextTurn", 3f);
        _isLoose = false;
        RoundTime = 0;
    }

    public void StopGame(float time)
    {
        CancelInvoke();
        if (_players != null)
        {
            _players.Clear();
        }
        _currentTurn = null;
        MainMenu.Show(time);
        SoundController.Stop(GameMusic);
        SoundController.Play(FinishSound);
    }

    private void NextTurn()
    {
        ICharacter previousCharacter = _currentTurn == null ? null : _currentTurn.Value;
        if (_currentTurn == null || _currentTurn.Next == null)
        {
            _currentTurn = _players.First;
        }
        else
        {
            _currentTurn = _currentTurn.Next;
        }
        Debug.Log("Start turn " + _currentTurn.Value.GetCharacter().Title);
 
        if (IsLoose())
        {
            _isLoose = true;
        }

        var variants = _currentTurn.Value.GetVariants(VariantsCount);
        if (OnActivePlayerChanged != null)
        {
            OnActivePlayerChanged(previousCharacter, _currentTurn.Value, variants);
        }

        if (_isLoose)
        {
            
            return;
        }
        

        if (_currentTurn.Value.IsHuman())
        {
            _currentTurn.Value.OnStartTurn();
        }
        else
        {
            Invoke("GenerateAnswer", 3f);
        }
    }

    private void GenerateAnswer()
    {
        _currentTurn.Value.OnStartTurn();
    }

    public void Answer(ICharacter from, SentenceObject sentence)
    {
        if(_currentTurn == null) return;
        if(!ReferenceEquals(_currentTurn.Value, from)) return;

        foreach (var player in _players)
        {
            player.OnSaid(from, sentence);
        }

        if (OnActivePlayerAnswered != null)
        {
            OnActivePlayerAnswered(_currentTurn.Value, sentence);
        }

        _currentTurn.Value.OnEndTurn();

        NextTurn();
    }

    private bool IsLoose()
    {
        bool hasNotFailed = false;
        bool hasFailed = false;
        foreach (var player in _players)
        {
            if (player.GetPatience() > 0)
            {
                hasNotFailed = true;
            }
            else
            {
                hasFailed = true;
            }
        }
        if (FailIfAllPlayersZero)
        {
            if (!hasNotFailed)
            {
                return true;
            }
        }
        else
        {
            if (hasFailed)
            {
                return true;
            }
        }
        return false;
    }

    private void OnPlayerLose()
    {
        StopGame(RoundTime);
    }

    private CharacterObject[] ChooseOpponents()
    {
        if (Opponents.Length < OpponentsCount)
        {
            throw new Exception("Не хватает оппонентов: " + Opponents.Length + "/" + OpponentsCount);
        }

        List<CharacterObject> choosed = new List<CharacterObject>();
        string opponentsTitles = "";

        int cantGenerateCounter = 0;
        while (choosed.Count < OpponentsCount && cantGenerateCounter < 1000)
        {
            int id = Random.Range(0, Opponents.Length);
            if (IsAllowToSameCharacters || !choosed.Contains(Opponents[id]))
            {
                choosed.Add(Opponents[id]);
                opponentsTitles += Opponents[id].Title + " ";
            }
            cantGenerateCounter++;
        }
        return choosed.ToArray();
    }

    private LinkedList<ICharacter> CreatePlayers(CharacterObject[] characters)
    {
        LinkedList<ICharacter> players = new LinkedList<ICharacter>();
        string queue = "";

        int humanTurn = Random.Range(0, characters.Length + 1);
        for (int i = 0, offset = 0; i < characters.Length + 1; i++)
        {
            if (i == humanTurn)
            {
                players.AddLast(new HumanPlayer(PlayerCharacter));
                queue += PlayerCharacter.Title + " ";
                offset++;
            }
            else
            {
                players.AddLast(new CpuPlayer(characters[i - offset]));
                queue += characters[i - offset].Title + " ";
            }
            players.Last.Value.OnAnswered += Answer;
        }

        Debug.Log("Turn queue: " + queue);
        return players;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            StopGame(RoundTime);
        }

        if (ROUND_TIME - RoundTime <= 0)
        {
            StopGame(RoundTime);
        }

        if (_isLoose == false)
        {
            RoundTime += Time.deltaTime;
        }
    }
}