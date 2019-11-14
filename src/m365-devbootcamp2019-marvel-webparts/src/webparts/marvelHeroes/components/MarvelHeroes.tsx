import * as React from 'react';
import styles from './MarvelHeroes.module.scss';
import { IMarvelHeroesProps } from './IMarvelHeroesProps';
import { escape } from '@microsoft/sp-lodash-subset';
import { IMarvelHeroesState } from './IMarvelHeroesState';
import { HeroeService } from '../../../services';
import { IHeroe } from '../../../model';

export default class MarvelHeroes extends React.Component<IMarvelHeroesProps, IMarvelHeroesState> {

  private _heroeService: HeroeService;

  constructor(props: IMarvelHeroesProps) {
    super(props);

    this._heroeService = new HeroeService(this.props.apiEndpoint, this.props.context.aadHttpClientFactory);

    this.state = {
      heroes: [],
      oops: null
    };
  }

  public componentDidMount(): void {
    this._heroeService.getHeroes().then((heroes: IHeroe[]) => {
      this.setState({
        heroes: heroes
      });
    }).catch(error => {
      console.log(error);
      this.setState({
        oops: 'Something went wrong... call the police!'
      });
    });
  }

  public render(): React.ReactElement<IMarvelHeroesProps> {


    return (
      <div className={ styles.marvelHeroes }>
        <div className={ styles.container }>
          <div className={ styles.row }>
            <div className={ styles.column }>
              <span className={ styles.title }>Welcome to SharePoint!</span>
              <p className={ styles.subTitle }>Customize SharePoint experiences using Web Parts.</p>
              <p className={ styles.description }>{escape(this.props.apiEndpoint)}</p>
              <a href="https://aka.ms/spfx" className={ styles.button }>
                <span className={ styles.label }>Learn more</span>
              </a>
            </div>
          </div>
        </div>
      </div>
    );
  }
}
