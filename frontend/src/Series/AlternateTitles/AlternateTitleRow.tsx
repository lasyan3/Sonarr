import classNames from 'classnames';
import React, { useCallback } from 'react';
import Label, { LabelProps } from 'Components/Label';
import IconButton from 'Components/Link/IconButton';
import TableRowCell from 'Components/Table/Cells/TableRowCell';
import TableRow from 'Components/Table/TableRow';
import { icons, kinds } from 'Helpers/Props';
import { AlternateTitle, AlternateTitleSource } from 'Series/Series';
import translate from 'Utilities/String/translate';
import styles from './AlternateTitleRow.css';

const sourceLabels: Record<
  AlternateTitleSource,
  { label: () => string; kind: LabelProps['kind'] }
> = {
  services: { label: () => 'Sonarr', kind: kinds.INFO },
  xem: { label: () => 'TheXEM', kind: kinds.INFO },
  custom: {
    label: () => translate('AlternateTitleSourceCustom'),
    kind: kinds.SUCCESS,
  },
  unknown: { label: () => translate('Unknown'), kind: kinds.DEFAULT },
};

function formatSeasonNumber(seasonNumber?: number | null) {
  return seasonNumber == null || seasonNumber < 0 ? '' : seasonNumber;
}

interface AlternateTitleRowProps {
  alternateTitle: AlternateTitle;
  isSaving: boolean;
  onDeletePress(id: number): void;
  onExcludePress(title: string, isExcluded: boolean): void;
}

function AlternateTitleRow({
  alternateTitle,
  isSaving,
  onDeletePress,
  onExcludePress,
}: AlternateTitleRowProps) {
  const {
    id,
    title,
    seasonNumber,
    sceneSeasonNumber,
    comment,
    source,
    isExcluded,
    isIgnored,
  } = alternateTitle;

  const sourceLabel = sourceLabels[source] ?? sourceLabels.unknown;

  let excludeTitle = isExcluded
    ? translate('ShowAlternateTitle')
    : translate('HideAlternateTitle');

  if (isIgnored) {
    excludeTitle = translate('AlternateTitleIgnored');
  }

  const handleDeletePress = useCallback(() => {
    onDeletePress(id);
  }, [id, onDeletePress]);

  const handleExcludePress = useCallback(() => {
    onExcludePress(title, !isExcluded);
  }, [title, isExcluded, onExcludePress]);

  return (
    <TableRow>
      <TableRowCell>
        <span
          className={classNames(
            styles.title,
            (isExcluded || isIgnored) && styles.excluded
          )}
        >
          {title}
        </span>

        {comment ? <span className={styles.comment}> {comment}</span> : null}
      </TableRowCell>

      <TableRowCell>{formatSeasonNumber(seasonNumber)}</TableRowCell>

      <TableRowCell>{formatSeasonNumber(sceneSeasonNumber)}</TableRowCell>

      <TableRowCell>
        <Label kind={sourceLabel.kind}>{sourceLabel.label()}</Label>
      </TableRowCell>

      <TableRowCell>
        {source === 'custom' ? (
          <IconButton
            name={icons.DELETE}
            title={translate('DeleteAlternateTitle')}
            isDisabled={isSaving}
            onPress={handleDeletePress}
          />
        ) : (
          <IconButton
            name={isExcluded ? icons.VIEW : icons.HIDDEN}
            title={excludeTitle}
            isDisabled={isSaving || isIgnored}
            onPress={handleExcludePress}
          />
        )}
      </TableRowCell>
    </TableRow>
  );
}

export default AlternateTitleRow;
