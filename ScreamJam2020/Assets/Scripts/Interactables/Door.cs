﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class Door : Interactable
{
    public delegate void OnDoorAction();
    public OnDoorAction OnDoorOpened;

    public bool disableCollider;
    public bool openOnce;
    [Header("Door Config"), Space]
    public bool isLocked;
    public bool isOpen;
    public bool canInteract = true;

    [Header("No Animation Config")]
    public GameObject openState;
    public GameObject closeState;

    [Header("Sound Config"),Space]
    public AudioClip openSound;
    public AudioClip closeSound;
    public AudioClip lockedSound;
    [HideInInspector]
    protected AudioSource audioSource;

    private Animator animator;

    public delegate void DoorEvent(bool isOpen);
    public event DoorEvent DoorInteract;

    // Start is called before the first frame update
    protected void Start()
    {
        audioSource = GetComponent<AudioSource>();
        animator = GetComponent<Animator>();

        if (animator)
        {
            if (isOpen)
            {
                animator.SetBool("StartOpen", true);
            }
            else
            {
                animator.SetBool("StartOpen", false);
            }
        }
        
        
    }

    public override void OnInteract()
    {
        InteractDoor();
    }

    public override void Activate(bool activate)
    {
        isLocked = !activate;

        if (isLocked && isOpen)
            ToggleDoor();
        if (!isLocked && !isOpen)
            ToggleDoor();
    }

    public void InteractDoor()
    {
        if (!isLocked && canInteract)
        {
            ToggleDoor();
        }
        else
        {
            if (audioSource)
            {
                audioSource.clip = lockedSound;
                audioSource.Play();
            }
        }
    }

    private void ToggleDoor()
    {
        isOpen = !isOpen;
        DoorInteract?.Invoke(isOpen);

        if (isOpen)
        {
            // Open
            OpenDoor();
        }
        else
        {
            // Close
            CloseDoor();
        }
    }

    private void CloseDoor()
    {
        if(animator)
        {
            animator.SetTrigger("Close");
        }
        else
        {
            closeState.SetActive(true);
            openState.SetActive(false);
        }

        if (disableCollider)
        {
            GetComponent<BoxCollider>().enabled = false;
        }

        canInteract = false;
        if (audioSource)
        {
            audioSource.clip = closeSound;
            audioSource.Play();
        }
    }

    private void OpenDoor()
    {
        if (animator)
        {
            animator.SetTrigger("Open");
        }
        else
        {
            closeState.SetActive(false);
            openState.SetActive(true);
        }

        if(disableCollider)
        {
            GetComponent<BoxCollider>().enabled = false;
        }
        
        canInteract = false;
        if (audioSource)
        {
            audioSource.clip = openSound;
            audioSource.Play();
        }

        if(OnDoorOpened != null)
        {
            OnDoorOpened();
        }
    }

    public void CanInteract()
    {
        canInteract = true;
    }

    public void CannotInteract()
    {
        canInteract = false;
    }
}
