using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponCollider : MonoBehaviour
{
    internal enum State
    {
        Normal,
        Lob
    }

    [SerializeField] LayerMask layermask;
    [SerializeField] float damage = 100f;
    [SerializeField] List<ParticleSystem> particles;
    [SerializeField] OnlinePlayer owner;
    [SerializeField] private bool isActive = false;
    [SerializeField] private List<Ball> touched = new List<Ball>();
    private State state;

    private State State1 { get => state; set => state = value; }
    public OnlinePlayer Owner { get => owner; set => owner = value; }

    private void Awake()
    {
        owner = GetComponentInParent<OnlinePlayer>();
    }


    private void OnTriggerStay(Collider other)
    {
        TryHitCollider(other);
    }
    private void OnTriggerEnter(Collider other)
    {
        TryHitCollider(other);
    }

    private void TryHitCollider(Collider other)
    {
        if (!isActive || !owner.IsOwner) return;

        var isBall = layermask == (layermask | (1 << other.gameObject.layer));

        if (!isBall) return;

        /* var victim = other.GetComponentInParent<OnlinePlayer>();

         if(victim && victim != owner && !touched.Contains(victim))
         {
             victim.GetHit(damage);
             touched.Add(victim);
         }*/

        var ball = other.GetComponentInParent<Ball>();

        if (ball && !touched.Contains(ball))
        {
            touched.Add(ball);
            switch (state)
            {
                case State.Normal:
                    if (owner.Controller.Grounded || owner.Controller.WallLeft || owner.Controller.WallRight)
                    {
                        if (ball.TryHitBall(owner, owner.hitDirection))
                        {
                            owner.SetSlowMo(true);
                            ball.OnIdleOverEvent.AddListener(delegate {
                                owner.SetSlowMo(false);
                                //ball.OnIdleOverEvent.RemoveAllListeners();
                            });
                        }
                    }
                    else
                    {
                        if (ball.TrySmashBall(owner, owner.hitDirection))
                        {
                            owner.SetSlowMo(true);
                            ball.OnIdleOverEvent.AddListener(delegate {
                                owner.SetSlowMo(false);
                                //ball.OnIdleOverEvent.RemoveAllListeners();
                            });
                        }
                    }

                    break;
                case State.Lob:
                    ball.TryLobBall(owner, owner.hitDirection);
                    break;
                default:
                    break;
            }
        }
    }

    internal void IsActive(bool v, State state = State.Normal)
    {
        isActive = v;
        this.state = state;
        if (!v) foreach (var particle in particles) particle.Stop(); else foreach (var particle in particles) particle.Play();
        if (v) touched.Clear();
    }
}
